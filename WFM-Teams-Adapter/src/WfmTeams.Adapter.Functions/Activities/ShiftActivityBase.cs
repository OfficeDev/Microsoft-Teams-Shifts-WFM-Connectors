// ---------------------------------------------------------------------------
// <copyright file="ShiftActivityBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Polly;
    using Polly.Retry;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.MicrosoftGraph.Exceptions;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public abstract class ShiftActivityBase : DeltaActivity<ShiftModel>
    {
        protected readonly IScheduleCacheService _scheduleCacheService;

        protected ShiftActivityBase(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<ShiftModel> deltaService, ICacheService cacheService, IScheduleCacheService scheduleCacheService)
            : base(options, wfmDataService, teamsService, deltaService, cacheService)
        {
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
        }

        protected static IDictionary<string, JobModel> BuildJobLookup(IEnumerable<ShiftModel> shifts)
        {
            return shifts
                .SelectMany(shift => shift.Jobs)
                .Where(job => !string.IsNullOrEmpty(job.WfmJobId) && !string.IsNullOrEmpty(job.Code))
                .GroupBy(job => job.WfmJobId)
                .Select(group => group.First())
                .ToDictionary(job => job.WfmJobId, job => new JobModel
                {
                    WfmId = job.WfmJobId,
                    Name = job.Code,
                    DepartmentName = job.DepartmentName,
                    ThemeCode = job.ThemeCode
                });
        }

        protected static IDictionary<string, string> BuildScheduleGroupLookup(List<ShiftModel> shifts)
        {
            return shifts
                .Where(s => s.DepartmentName != null && !string.IsNullOrEmpty(s.TeamsSchedulingGroupId))
                .GroupBy(s => s.DepartmentName)
                .ToDictionary(s => s.Key, s => s.First().TeamsSchedulingGroupId);
        }

        protected static List<string> GetAllUsersInDepartment(IEnumerable<ShiftModel> allShifts, string department)
        {
            return allShifts.Where(s => s.DepartmentName == department && !string.IsNullOrEmpty(s.TeamsEmployeeId)).Select(s => s.TeamsEmployeeId).Distinct().ToList();
        }

        protected override async Task<CacheModel<ShiftModel>> GetSavedRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            return await _scheduleCacheService.LoadScheduleAsync(GetSaveScheduleId(activityModel.TeamId), activityModel.StartDate).ConfigureAwait(false);
        }

        protected abstract string GetSaveScheduleId(string teamId);

        protected override void LogRecordError(Exception ex, TeamActivityModel activityModel, string operation, ShiftModel record, ILogger log)
        {
            log.LogShiftError(ex, activityModel, operation, record);
        }

        protected override void LogRecordSkipped(TeamActivityModel activityModel, string operation, ShiftModel record, ILogger log)
        {
            log.LogShiftSkipped(activityModel, operation, record);
        }

        protected override Task SaveRecordsAsync(TeamActivityModel activityModel, CacheModel<ShiftModel> savedRecords)
        {
            // not implemented as the records are saved via the UpdateSavedRecords override instead
            return Task.CompletedTask;
        }

        protected async Task SetJobAndDepartmentNameAsync(IEnumerable<ShiftModel> shifts, IDictionary<string, JobModel> jobLookup, TeamActivityModel activityModel, ILogger log)
        {
            var activities = shifts
                .SelectMany(s => s.Jobs)
                .Where(a => !string.IsNullOrEmpty(a.WfmJobId));

            foreach (var activity in activities)
            {
                if (!jobLookup.TryGetValue(activity.WfmJobId, out var job))
                {
                    try
                    {
                        job = await _wfmDataService.GetJobAsync(activityModel.TeamId, activityModel.WfmBuId, activity.WfmJobId)
                            ?? throw new KeyNotFoundException();

                        jobLookup[activity.WfmJobId] = job;
                    }
                    catch (Exception)
                    {
                        log.LogJobNotFound(activityModel, activity);
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

        protected async Task SetTeamsSchedulingGroupIdAsync(IEnumerable<ShiftModel> shifts, TeamActivityModel activityModel, ILogger log)
        {
            var groupLookup = new Dictionary<string, string>();

            foreach (var shift in shifts)
            {
                if (string.IsNullOrEmpty(shift.DepartmentName))
                {
                    log.LogDepartmentNotFound(activityModel, shift);
                    continue;
                }

                if (!groupLookup.ContainsKey(shift.DepartmentName))
                {
                    try
                    {
                        // first attempt to get an existing group with the department name
                        var groupId = await _teamsService.GetSchedulingGroupIdByNameAsync(activityModel.TeamId, shift.DepartmentName).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(groupId))
                        {
                            continue;
                        }

                        groupLookup.Add(shift.DepartmentName, groupId);
                    }
                    catch (MicrosoftGraphException e)
                    {
                        log.LogSchedulingGroupError(e, activityModel, shift);
                        continue;
                    }
                    catch (Exception e)
                    {
                        log.LogSchedulingGroupError(e, activityModel, shift);
                        continue;
                    }
                }

                shift.TeamsSchedulingGroupId = groupLookup[shift.DepartmentName];
            }
        }

        protected override async Task UpdateSavedRecordsAsync(TeamActivityModel activityModel, CacheModel<ShiftModel> savedRecords, DeltaModel<ShiftModel> delta)
        {
            var policy = GetConflictRetryPolicy(_options.RetryMaxAttempts, _options.RetryIntervalSeconds);
            await policy.ExecuteAsync(() => UpdateScheduleAsync(activityModel, delta));
        }

        private static AsyncRetryPolicy GetConflictRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<StorageException>(h => h.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict)
                .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(retryInterval));
        }

        private async Task UpdateScheduleAsync(TeamActivityModel activityModel, DeltaModel<ShiftModel> delta)
        {
            // apply the final delta to the current version of the savedShifts ensuring that no
            // other process can update it while we do so - N.B. the minimum lease time is 15s, the
            // maximum lease time is 1m
            var savedShifts = await _scheduleCacheService.LoadScheduleWithLeaseAsync(GetSaveScheduleId(activityModel.TeamId), activityModel.StartDate, new TimeSpan(0, 0, _options.StorageLeaseTimeSeconds));
            delta.ApplyChanges(savedShifts.Tracked);
            delta.ApplySkipped(savedShifts.Skipped);
            await _scheduleCacheService.SaveScheduleWithLeaseAsync(GetSaveScheduleId(activityModel.TeamId), activityModel.StartDate, savedShifts);
        }
    }
}
