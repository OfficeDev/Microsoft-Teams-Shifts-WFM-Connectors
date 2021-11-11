// ---------------------------------------------------------------------------
// <copyright file="ShiftsWeekActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ShiftsWeekActivity : ShiftActivityBase
    {
        public ShiftsWeekActivity(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<ShiftModel> deltaService, ICacheService cacheService, IScheduleCacheService scheduleCacheService)
            : base(options, wfmDataService, teamsService, deltaService, cacheService, scheduleCacheService)
        {
        }

        [FunctionName(nameof(ShiftsWeekActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] WeekModel weekModel, ILogger log)
        {
            var activityModel = new TeamActivityModel
            {
                TeamId = weekModel.TeamId,
                DateValue = weekModel.StartDate.AsDateString(),
                ActivityType = "Shifts",
                WfmBuId = weekModel.WfmBuId,
                StartDate = weekModel.StartDate,
                TimeZoneInfoId = weekModel.TimeZoneInfoId
            };

            return await RunDeltaActivity(activityModel, log).ConfigureAwait(false);
        }

        protected override async Task ApplyDeltaAsync(TeamActivityModel activityModel, DeltaModel<ShiftModel> delta, ILogger log)
        {
            await UpdateDestinationAsync(nameof(delta.Created), activityModel, delta, delta.Created, _teamsService.CreateShiftAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Updated), activityModel, delta, delta.Updated, _teamsService.UpdateShiftAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Deleted), activityModel, delta, delta.Deleted, _teamsService.DeleteShiftAsync, log).ConfigureAwait(false);
        }

        protected override string GetSaveScheduleId(string teamId)
        {
            return teamId;
        }

        protected override async Task<List<ShiftModel>> GetSourceRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            return await _wfmDataService.ListWeekShiftsAsync(activityModel.TeamId, activityModel.WfmBuId, activityModel.StartDate, activityModel.TimeZoneInfoId).ConfigureAwait(false);
        }

        protected override async Task SetTeamsIdsAsync(DeltaModel<ShiftModel> delta, CacheModel<ShiftModel> savedRecords, TeamActivityModel activityModel, ILogger log)
        {
            var allRecords = delta.All;

            // set teams employee
            await SetTeamsEmployeeIdsAsync(allRecords.Where(s => string.IsNullOrEmpty(s.TeamsEmployeeId)), activityModel.TeamId).ConfigureAwait(false);

            // set job & department name
            var jobLookup = BuildJobLookup(savedRecords.Tracked);
            await SetJobAndDepartmentNameAsync(allRecords, jobLookup, activityModel, log).ConfigureAwait(false);

            // set teams schedule group (N.B this must be set after teams employee id and jobs)
            await SetTeamsSchedulingGroupIdAsync(allRecords.Where(s => string.IsNullOrEmpty(s.TeamsSchedulingGroupId) && !string.IsNullOrEmpty(s.TeamsEmployeeId)), activityModel, log).ConfigureAwait(false);
            await AddEmployeesToSchedulingGroupsAsync(delta, activityModel, log).ConfigureAwait(false);
        }

        private async Task AddEmployeesToSchedulingGroupsAsync(DeltaModel<ShiftModel> delta, TeamActivityModel activityModel, ILogger log)
        {
            var allShifts = delta.All;
            var groupLookup = BuildScheduleGroupLookup(allShifts);

            foreach (var department in groupLookup.Keys)
            {
                // get all the user id's in this department
                var userIds = GetAllUsersInDepartment(allShifts, department);
                if (userIds.Count > 0)
                {
                    try
                    {
                        // and add them to the matching schedule group if necessary
                        await _teamsService.AddUsersToSchedulingGroupAsync(activityModel.TeamId, groupLookup[department], userIds).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        delta.Created.Concat(delta.Updated).Where(i => i.DepartmentName == department).ForEach(i => delta.FailedChange(i));
                        log.LogSchedulingGroupError(e, activityModel, department, groupLookup[department]);
                        continue;
                    }
                }
            }
        }
    }
}
