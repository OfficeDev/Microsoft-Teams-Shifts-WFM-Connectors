using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Options;
using JdaTeams.Connector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class TeamHealthTrigger
    {
        private readonly ConnectorOptions _options;
        private readonly ISecretsService _secretsService;
        private readonly IScheduleSourceService _scheduleSourceService;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleDestinationService _scheduleDestinationService;
        private readonly IScheduleCacheService _scheduleCacheService;

        public TeamHealthTrigger(ConnectorOptions options, ISecretsService secretsService, IScheduleSourceService scheduleSourceService, IScheduleConnectorService scheduleConnectorService, IScheduleDestinationService scheduleDestinationService, IScheduleCacheService scheduleCacheService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
        }

        [FunctionName(nameof(TeamHealthTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = "teamHealth/{teamId}")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            try
            {
                var connection = await _scheduleConnectorService.GetConnectionAsync(teamId);
                var credentials = await _secretsService.GetCredentialsAsync(connection.TeamId);
                _scheduleSourceService.SetCredentials(connection.TeamId, credentials);

                var date = DateTime.Today;
                if (req.Query.ContainsKey("date"))
                {
                    DateTime.TryParse(req.Query["date"], out date);
                }
                var weekStartDate = date.StartOfWeek(_options.StartDayOfWeek);

                var jdaEmployees = await _scheduleSourceService.GetEmployeesAsync(connection.TeamId, connection.StoreId, weekStartDate);
                await UpdateEmployeesWithTeamsData(connection.TeamId, jdaEmployees);

                var cachedShifts = await GetCachedShiftsAsync(connection, weekStartDate);
                var jobIds = cachedShifts.SelectMany(s => s.Jobs).Select(j => j.JdaJobId).Distinct().ToList();
                var jobs = await GetJobsAsync(connection, jobIds);
                ExpandIds(cachedShifts, jdaEmployees, jobs);

                var missingUsers = jdaEmployees.Where(e => string.IsNullOrEmpty(e.DestinationId)).ToList();

                var missingShifts = await GetMissingShifts(connection, weekStartDate, cachedShifts);
                jobIds = missingShifts.SelectMany(s => s.Jobs).Select(j => j.JdaJobId).Distinct().ToList();
                jobs = await GetJobsAsync(connection, jobIds);
                ExpandIds(missingShifts, missingUsers, jobs);

                var teamHealthResponseModel = new TeamHealthResponseModel
                {
                    TeamId = connection.TeamId,
                    WeekStartDate = weekStartDate,
                    TeamOrchestratorStatus = await starter.GetStatusAsync(connection.TeamId),
                    MissingUsers = missingUsers,
                    MissingShifts = missingShifts,
                    CachedShifts = cachedShifts
                };

                return new JsonResult(teamHealthResponseModel, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            catch(Exception ex)
            {
                log.LogError(ex, "Team health failed!");
                return new ContentResult
                {
                    Content = $"Unexpected exception: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        private async Task<List<ShiftModel>> GetCachedShiftsAsync(ConnectionModel connection, DateTime weekStartDate)
        {
            var cachedShifts = await _scheduleCacheService.LoadScheduleAsync(connection.TeamId, weekStartDate);
            return cachedShifts.Tracked.OrderBy(s => s.StartDate).ToList();
        }

        private async Task<List<JobModel>> GetJobsAsync(ConnectionModel connection, List<string> jobIds)
        {
            var tasks = jobIds.Select(jobId => _scheduleSourceService.GetJobAsync(connection.TeamId, connection.StoreId, jobId));
            var jobs = await Task.WhenAll(tasks);

            return jobs.ToList();
        }

        private async Task UpdateEmployeesWithTeamsData(string teamId, List<EmployeeModel> jdaEmployees)
        {
            // update the employee with data from Teams
            var tasks = jdaEmployees.Select(employee => UpdateJdaEmployeeFromTeamsAsync(teamId, employee));
            await Task.WhenAll(tasks);
        }

        private async Task UpdateJdaEmployeeFromTeamsAsync(string teamId, EmployeeModel jdaEmployee)
        {
            var teamEmployee = await _scheduleDestinationService.GetEmployeeAsync(teamId, jdaEmployee.LoginName);
            if(teamEmployee != null)
            {
                jdaEmployee.DestinationId = teamEmployee.DestinationId;
                jdaEmployee.DisplayName = teamEmployee.DisplayName;
            }

        }

        private async Task<List<ShiftModel>> GetMissingShifts(ConnectionModel connection, DateTime weekStartDate, List<ShiftModel> cachedShifts)
        {
            // get the shifts for the week from JDA
            var jdaShifts = await _scheduleSourceService.ListWeekShiftsAsync(connection.TeamId, connection.StoreId, weekStartDate, connection.TimeZoneInfoId);
            var cachedShiftIds = cachedShifts.Select(s => s.JdaShiftId).ToList();

            return jdaShifts.Where(s => !cachedShiftIds.Contains(s.JdaShiftId)).ToList();
        }

        private void ExpandIds(List<ShiftModel> shifts, List<EmployeeModel> employees, List<JobModel> jobs)
        {
            foreach (var shift in shifts)
            {
                shift.JdaEmployeeName = employees.First(e => e.SourceId == shift.JdaEmployeeId.ToString()).DisplayName;
                var shiftJob = jobs.First(j => j.SourceId == shift.JdaJobId);
                shift.DepartmentName = shiftJob.DepartmentName;
                shift.JdaJobName = shiftJob.Name;
                foreach(var job in shift.Jobs)
                {
                    job.Code = job.Code ?? jobs.First(j => j.SourceId == job.JdaJobId).Name;
                }
            }
        }
    }
}
