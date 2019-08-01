using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.MicrosoftGraph.Exceptions;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Activities
{
    public class WeekActivity
    {
        private readonly WeekActivityOptions _options;
        private readonly IScheduleSourceService _scheduleSourceService;
        private readonly IScheduleCacheService _scheduleCacheService;
        private readonly IScheduleDestinationService _scheduleDestinationService;
        private readonly IScheduleDeltaService _scheduleDeltaService;
        private readonly ISecretsService _secretsService;

        public WeekActivity(WeekActivityOptions options, IScheduleSourceService scheduleSourceService, IScheduleCacheService scheduleCacheService,
            IScheduleDestinationService scheduleDestinationService, IScheduleDeltaService scheduleDeltaService, ISecretsService secretsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
            _scheduleDeltaService = scheduleDeltaService ?? throw new ArgumentNullException(nameof(scheduleDeltaService));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        [FunctionName(nameof(WeekActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] WeekModel weekModel, ILogger log)
        {
            log.LogWeek(weekModel);

            // initialise source service
            var credentials = await _secretsService.GetCredentialsAsync(weekModel.TeamId);
            _scheduleSourceService.SetCredentials(weekModel.TeamId, credentials);

            // get the current set of shifts from JDA
            var shifts = await _scheduleSourceService.ListWeekShiftsAsync(weekModel.TeamId, weekModel.StoreId, weekModel.StartDate);

            log.LogShifts(weekModel, shifts);

            // get the last saved set of shifts
            var savedShifts = await _scheduleCacheService.LoadScheduleAsync(weekModel.TeamId, weekModel.StartDate);

            // compute the delta
            var delta = _scheduleDeltaService.ComputeDelta(savedShifts.Tracked, shifts);

            log.LogFullDelta(weekModel, delta);

            if (delta.HasChanges)
            {
                delta.RemoveSkipped(savedShifts.Skipped);
                delta.ApplyMaximum(_options.MaximumDelta);

                log.LogPartialDelta(weekModel, delta);

                var allShifts = delta.All;

                // set teams employee
                var employeeLookup = BuildEmployeeLookup(savedShifts.Tracked);
                await SetTeamsEmployeeIds(allShifts.Where(s => string.IsNullOrEmpty(s.TeamsEmployeeId)), employeeLookup, weekModel, log);

                // set job & department name
                var jobLookup = BuildJobLookup(savedShifts.Tracked);
                await SetJobAndDepartmentName(allShifts, jobLookup, weekModel, log);

                // set teams schedule group (N.B this must be set after teams employee id and jobs)
                var groupLookup = BuildScheduleGroupLookup(savedShifts.Tracked);
                await SetTeamsSchedulingGroupId(allShifts.Where(s => string.IsNullOrEmpty(s.TeamsSchedulingGroupId)), groupLookup, weekModel, log);
                await AddEmployeesToSchedulingGroups(delta, groupLookup, weekModel, log);

                // update teams
                await ApplyDeltaAsync(weekModel, delta, log);

                log.LogAppliedDelta(weekModel, delta);

                // apply the final delta to the savedShifts
                delta.ApplyChanges(savedShifts.Tracked);
                delta.ApplySkipped(savedShifts.Skipped);

                await _scheduleCacheService.SaveScheduleAsync(weekModel.TeamId, weekModel.StartDate, savedShifts);
            }

            return delta.AsResult();
        }

        private IDictionary<int, string> BuildEmployeeLookup(IEnumerable<ShiftModel> shifts)
        {
            return shifts
                .Where(i => !string.IsNullOrEmpty(i.TeamsEmployeeId))
                .GroupBy(i => i.JdaEmployeeId)
                .ToDictionary(i => i.Key, i => i.First().TeamsEmployeeId);
        }

        private async Task SetTeamsEmployeeIds(IEnumerable<ShiftModel> shifts, IDictionary<int, string> employeeLookup, WeekModel weekModel, ILogger log)
        {
            await _scheduleSourceService.LoadEmployeesAsync(weekModel.TeamId, shifts.Select(s => s.JdaEmployeeId.ToString()).Distinct().ToList());

            foreach (var shift in shifts)
            {
                if (!employeeLookup.ContainsKey(shift.JdaEmployeeId))
                {
                    EmployeeModel jdaEmployee;

                    try
                    {
                        jdaEmployee = await _scheduleSourceService.GetEmployeeAsync(weekModel.TeamId, shift.JdaEmployeeId.ToString())
                            ?? throw new KeyNotFoundException();
                    }
                    catch (Exception)
                    {
                        log.LogEmployeeNotFound(weekModel, shift);
                        continue;
                    }

                    try
                    {
                        var graphEmployee = await _scheduleDestinationService.GetEmployeeAsync(weekModel.TeamId, jdaEmployee.LoginName)
                            ?? throw new KeyNotFoundException();

                        employeeLookup.Add(shift.JdaEmployeeId, graphEmployee.DestinationId);
                    }
                    catch (KeyNotFoundException)
                    {
                        employeeLookup.Add(shift.JdaEmployeeId, null);
                    }
                    catch (Exception e)
                    {
                        log.LogMemberError(e, weekModel, jdaEmployee);

                        employeeLookup.Add(shift.JdaEmployeeId, null);
                    }
                }

                shift.TeamsEmployeeId = employeeLookup[shift.JdaEmployeeId];
            }
        }

        private IDictionary<string, JobModel> BuildJobLookup(IEnumerable<ShiftModel> shifts)
        {
            return shifts
                .SelectMany(shift => shift.Jobs)
                .Where(job => !string.IsNullOrEmpty(job.JdaJobId) && !string.IsNullOrEmpty(job.Code))
                .GroupBy(job => job.JdaJobId)
                .Select(group => group.First())
                .ToDictionary(job => job.JdaJobId, job => new JobModel
                {
                    SourceId = job.JdaJobId,
                    Name = job.Code,
                    DepartmentName = job.DepartmentName,
                    ThemeCode = job.ThemeCode
                });
        }

        private async Task SetJobAndDepartmentName(IEnumerable<ShiftModel> shifts, IDictionary<string, JobModel> jobLookup, WeekModel weekModel, ILogger log)
        {
            var activities = shifts
                .SelectMany(s => s.Jobs)
                .Where(a => !string.IsNullOrEmpty(a.JdaJobId));

            foreach (var activity in activities)
            {
                if (!jobLookup.TryGetValue(activity.JdaJobId, out var job))
                {
                    try
                    {
                        job = await _scheduleSourceService.GetJobAsync(weekModel.TeamId, weekModel.StoreId, activity.JdaJobId)
                            ?? throw new KeyNotFoundException();

                        jobLookup[activity.JdaJobId] = job;
                    }
                    catch (Exception)
                    {
                        log.LogJobNotFound(weekModel, activity);
                        continue;
                    }
                }

                activity.Code = job.Name;
                activity.DepartmentName = job.DepartmentName;
                activity.ThemeCode = job.ThemeCode;
            }

            foreach (var shift in shifts)
            {
                var firstJob = shift.Jobs
                    .OrderBy(j => j.StartDate)
                    .FirstOrDefault();
                shift.DepartmentName = firstJob?.DepartmentName;
                shift.ThemeCode = firstJob?.ThemeCode;
            }
        }

        private IDictionary<string, string> BuildScheduleGroupLookup(List<ShiftModel> shifts)
        {
            return shifts
                .Where(s => s.DepartmentName != null && !string.IsNullOrEmpty(s.TeamsSchedulingGroupId))
                .GroupBy(s => s.DepartmentName)
                .ToDictionary(s => s.Key, s => s.First().TeamsSchedulingGroupId);
        }

        private async Task SetTeamsSchedulingGroupId(IEnumerable<ShiftModel> shifts, IDictionary<string, string> groupLookup, WeekModel weekModel, ILogger log)
        {
            foreach (var shift in shifts)
            {
                if (string.IsNullOrEmpty(shift.DepartmentName))
                {
                    log.LogDepartmentNotFound(weekModel, shift);
                    continue;
                }

                if (!groupLookup.ContainsKey(shift.DepartmentName))
                {
                    try
                    {
                        // first attempt to get an existing group with the department name
                        var groupId = await _scheduleDestinationService.GetSchedulingGroupIdByNameAsync(weekModel.TeamId, shift.DepartmentName);
                        if (string.IsNullOrEmpty(groupId))
                        {
                            // the group with the specified name does not exist, so create it, populated with all users from the shifts
                            // collection having this department
                            var userIds = GetAllUsersInDepartment(shifts, shift.DepartmentName);
                            groupId = await _scheduleDestinationService.CreateSchedulingGroupAsync(weekModel.TeamId, shift.DepartmentName, userIds);
                        }

                        groupLookup.Add(shift.DepartmentName, groupId);
                    }
                    catch (MicrosoftGraphException e)
                    {
                        log.LogSchedulingGroupError(e, weekModel, shift);
                        continue;
                    }
                    catch (Exception e)
                    {
                        log.LogSchedulingGroupError(e, weekModel, shift);
                        continue;
                    }
                }

                shift.TeamsSchedulingGroupId = groupLookup[shift.DepartmentName];
            }
        }

        private async Task AddEmployeesToSchedulingGroups(DeltaModel delta, IDictionary<string, string> groupLookup, WeekModel weekModel, ILogger log)
        {
            var allShifts = delta.All;
            foreach (var department in groupLookup.Keys)
            {
                // get all the user id's in this department
                var userIds = GetAllUsersInDepartment(allShifts, department);
                if (userIds.Count > 0)
                {
                    try
                    {
                        // and add them to the matching schedule group if necessary
                        await _scheduleDestinationService.AddUsersToSchedulingGroupAsync(weekModel.TeamId, groupLookup[department], userIds);
                    }
                    catch (Exception e)
                    {
                        delta.Created.Concat(delta.Updated).Where(i => i.DepartmentName == department).ForEach(i => delta.FailedChange(i));
                        log.LogSchedulingGroupError(e, weekModel, department, groupLookup[department]);
                        continue;
                    }
                }
            }
        }

        private List<string> GetAllUsersInDepartment(IEnumerable<ShiftModel> allShifts, string department)
        {
            return allShifts.Where(s => s.DepartmentName == department && !string.IsNullOrEmpty(s.TeamsEmployeeId)).Select(s => s.TeamsEmployeeId).Distinct().ToList();
        }

        private async Task ApplyDeltaAsync(WeekModel weekModel, DeltaModel delta, ILogger log)
        {
            await UpdateDestinationAsync(nameof(delta.Created), weekModel, delta, delta.Created, _scheduleDestinationService.CreateShiftAsync, log);
            await UpdateDestinationAsync(nameof(delta.Updated), weekModel, delta, delta.Updated, _scheduleDestinationService.UpdateShiftAsync, log);
            await UpdateDestinationAsync(nameof(delta.Deleted), weekModel, delta, delta.Deleted, _scheduleDestinationService.DeleteShiftAsync, log);
        }

        private async Task UpdateDestinationAsync(string operation, WeekModel weekModel, DeltaModel delta, IEnumerable<ShiftModel> shifts, Func<string, ShiftModel, Task> destinationMethod, ILogger log)
        {
            var tasks = shifts
                .Select(shift => UpdateDestinationAsync(operation, weekModel, delta, shift, destinationMethod, log))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task UpdateDestinationAsync(string operation, WeekModel weekModel, DeltaModel delta, ShiftModel shift, Func<string, ShiftModel, Task> destinationMethod, ILogger log)
        {
            try
            {
                await destinationMethod(weekModel.TeamId, shift);
            }
            catch (ArgumentException)
            {
                delta.SkippedChange(shift);
                log.LogShiftSkipped(weekModel, operation, shift);
            }
            catch (MicrosoftGraphException ex)
            {
                delta.FailedChange(shift);
                log.LogShiftError(ex, weekModel, operation, shift);
            }
            catch (Exception ex)
            {
                delta.FailedChange(shift);
                log.LogShiftError(ex, weekModel, operation, shift);
            }
        }
    }
}