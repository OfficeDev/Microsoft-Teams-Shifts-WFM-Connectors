// ---------------------------------------------------------------------------
// <copyright file="TeamHealthTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Options;
    using WfmTeams.Adapter.Services;

    public class TeamHealthTrigger
    {
        private readonly ICacheService _cacheService;
        private readonly FeatureOptions _featureOptions;
        private readonly ConnectorOptions _options;
        private readonly IScheduleCacheService _scheduleCacheService;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly ITeamsService _teamsService;
        private readonly IWfmDataService _wfmDataService;

        public TeamHealthTrigger(FeatureOptions featureOptions, ConnectorOptions options, IWfmDataService wfmDataService, IScheduleConnectorService scheduleConnectorService, ITeamsService teamsService, IScheduleCacheService scheduleCacheService, ICacheService cacheService)
        {
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        [FunctionName(nameof(TeamHealthTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "teamHealth/{teamId}")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            try
            {
                var connection = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);

                var date = DateTime.Today;
                if (req.Query.ContainsKey("date"))
                {
                    DateTime.TryParse(req.Query["date"], out date);
                }
                var weekStartDate = date.StartOfWeek(_options.StartDayOfWeek);

                var employees = await _wfmDataService.GetEmployeesAsync(connection.TeamId, connection.WfmBuId, weekStartDate).ConfigureAwait(false);
                await UpdateEmployeesWithTeamsData(connection.TeamId, employees).ConfigureAwait(false);

                var cachedShifts = await GetCachedShiftsAsync(connection, weekStartDate).ConfigureAwait(false);
                var jobIds = cachedShifts.SelectMany(s => s.Jobs).Select(j => j.WfmJobId).Distinct().ToList();
                var jobs = await GetJobsAsync(connection, jobIds).ConfigureAwait(false);
                ExpandIds(cachedShifts, employees, jobs);

                var missingUsers = employees.Where(e => string.IsNullOrEmpty(e.TeamsEmployeeId)).ToList();

                var missingShifts = await GetMissingShiftsAsync(connection, weekStartDate, cachedShifts).ConfigureAwait(false);
                jobIds = missingShifts.SelectMany(s => s.Jobs).Select(j => j.WfmJobId).Distinct().ToList();
                jobs = await GetJobsAsync(connection, jobIds).ConfigureAwait(false);
                ExpandIds(missingShifts, employees, jobs);

                var mappedUsers = await GetMappedUsersAsync(connection.TeamId).ConfigureAwait(false);

                var teamHealthResponseModel = new TeamHealthResponseModel
                {
                    TeamId = connection.TeamId,
                    WeekStartDate = weekStartDate.AsDateString(),
                    EmployeeCacheOrchestratorStatus = await starter.GetStatusAsync(string.Format(EmployeeCacheOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false),
                    EmployeeTokenRefreshOrchestratorStatus = await starter.GetStatusAsync(string.Format(EmployeeTokenRefreshOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false),
                    TeamOrchestratorStatus = await starter.GetStatusAsync(string.Format(ShiftsOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false),
                    MappedUsers = mappedUsers,
                    MissingUsers = missingUsers,
                    MissingShifts = missingShifts,
                    CachedShifts = cachedShifts
                };

                if (_featureOptions.EnableOpenShiftSync)
                {
                    teamHealthResponseModel.OpenShiftsOrchestratorStatus = await starter.GetStatusAsync(string.Format(OpenShiftsOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false);
                }

                if (_featureOptions.EnableTimeOffSync)
                {
                    teamHealthResponseModel.TimeOffOrchestratorStatus = await starter.GetStatusAsync(string.Format(TimeOffOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false);
                }

                if (_featureOptions.EnableAvailabilitySync)
                {
                    teamHealthResponseModel.AvailabilityOrchestratorStatus = await starter.GetStatusAsync(string.Format(AvailabilityOrchestrator.InstanceIdPattern, connection.TeamId)).ConfigureAwait(false);
                }

                // N.B. the following block returns the JSON in a ContentResult rather than in the
                // rather more concise JsonResult because to return the Json with the settings
                // required adding a package dependency to Microsoft.AspNetCore.Mvc.NewtonsoftJson
                // as per https://github.com/Azure/azure-functions-core-tools/issues/1907 which then
                // caused an issue with incompatible dependencies and a significant issue with
                // deserializing json in HttpRequestExtensions
                var json = JsonConvert.SerializeObject(teamHealthResponseModel, Formatting.Indented);
                return new ContentResult
                {
                    Content = json,
                    ContentType = "application/json",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Team health failed!");
                return new ContentResult
                {
                    Content = $"Unexpected exception: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        private void ExpandIds(List<ShiftModel> shifts, List<EmployeeModel> employees, List<JobModel> jobs)
        {
            foreach (var shift in shifts)
            {
                var employee = employees.FirstOrDefault(e => e.WfmEmployeeId == shift.WfmEmployeeId);
                if (employee != null)
                {
                    shift.WfmEmployeeName = employee.DisplayName;
                }
                else
                {
                    shift.WfmEmployeeName = "Unknown";
                }
                var shiftJob = jobs.First(j => j.WfmId == shift.WfmJobId);
                shift.DepartmentName = shiftJob.DepartmentName;
                shift.WfmJobName = shiftJob.Name;
                foreach (var job in shift.Jobs)
                {
                    job.Code ??= jobs.First(j => j.WfmId == job.WfmJobId).Name;
                }
            }
        }

        private async Task<List<ShiftModel>> GetCachedShiftsAsync(ConnectionModel connection, DateTime weekStartDate)
        {
            var cachedShifts = await _scheduleCacheService.LoadScheduleAsync(connection.TeamId, weekStartDate).ConfigureAwait(false);
            return cachedShifts.Tracked.OrderBy(s => s.StartDate).ToList();
        }

        private async Task<List<JobModel>> GetJobsAsync(ConnectionModel connection, List<string> jobIds)
        {
            var tasks = jobIds.Select(jobId => _wfmDataService.GetJobAsync(connection.TeamId, connection.WfmBuId, jobId));
            var jobs = await Task.WhenAll(tasks).ConfigureAwait(false);

            return jobs.ToList();
        }

        private async Task<List<EmployeeModel>> GetMappedUsersAsync(string teamId)
        {
            var users = new List<EmployeeModel>();

            // get the list of users from cache
            var userIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamId).ConfigureAwait(false);
            if (userIds != null)
            {
                foreach (var userId in userIds)
                {
                    var user = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, userId).ConfigureAwait(false);
                    if (user != null)
                    {
                        users.Add(user);
                    }
                }
            }

            return users;
        }

        private async Task<List<ShiftModel>> GetMissingShiftsAsync(ConnectionModel connection, DateTime weekStartDate, List<ShiftModel> cachedShifts)
        {
            // get the shifts for the week from WFM
            var shifts = await _wfmDataService.ListWeekShiftsAsync(connection.TeamId, connection.WfmBuId, weekStartDate, connection.TimeZoneInfoId).ConfigureAwait(false);
            var cachedShiftIds = cachedShifts.Select(s => s.WfmShiftId).ToList();

            return shifts.Where(s => !cachedShiftIds.Contains(s.WfmShiftId)).ToList();
        }

        private async Task UpdateEmployeesWithTeamsData(string teamId, List<EmployeeModel> employees)
        {
            // update the employee with data from Teams
            var tasks = employees.Select(employee => UpdateWfmEmployeeFromTeamsAsync(teamId, employee));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task UpdateWfmEmployeeFromTeamsAsync(string teamId, EmployeeModel employee)
        {
            var teamEmployee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, employee.WfmEmployeeId).ConfigureAwait(false);
            if (teamEmployee != null)
            {
                employee.TeamsEmployeeId = teamEmployee.TeamsEmployeeId;
                employee.DisplayName = teamEmployee.DisplayName;
            }
        }
    }
}
