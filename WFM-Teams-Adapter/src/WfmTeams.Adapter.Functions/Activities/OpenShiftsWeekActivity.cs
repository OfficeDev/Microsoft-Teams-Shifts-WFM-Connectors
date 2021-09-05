// ---------------------------------------------------------------------------
// <copyright file="OpenShiftsWeekActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class OpenShiftsWeekActivity : ShiftActivityBase
    {
        public OpenShiftsWeekActivity(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<ShiftModel> deltaService, ICacheService cacheService, IScheduleCacheService scheduleCacheService)
            : base(options, wfmDataService, teamsService, deltaService, cacheService, scheduleCacheService)
        {
        }

        [FunctionName(nameof(OpenShiftsWeekActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] WeekModel weekModel, ILogger log)
        {
            var activityModel = new TeamActivityModel
            {
                TeamId = weekModel.TeamId,
                DateValue = weekModel.StartDate.AsDateString(),
                ActivityType = "OpenShifts",
                WfmBuId = weekModel.WfmBuId,
                StartDate = weekModel.StartDate,
                TimeZoneInfoId = weekModel.TimeZoneInfoId
            };

            return await RunDeltaActivity(activityModel, log);
        }

        protected override async Task ApplyDeltaAsync(TeamActivityModel activityModel, DeltaModel<ShiftModel> delta, ILogger log)
        {
            await UpdateDestinationAsync(nameof(delta.Created), activityModel, delta, delta.Created, _teamsService.CreateOpenShiftAsync, log);
            await UpdateDestinationAsync(nameof(delta.Updated), activityModel, delta, delta.Updated, _teamsService.UpdateOpenShiftAsync, log);
            await UpdateDestinationAsync(nameof(delta.Deleted), activityModel, delta, delta.Deleted, _teamsService.DeleteOpenShiftAsync, log);
        }

        protected override string GetSaveScheduleId(string teamId)
        {
            return teamId + "_OS";
        }

        protected override async Task<List<ShiftModel>> GetSourceRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            var shifts = new List<ShiftModel>();

            // get a manager user for this business unit as only managers can get the schedule data
            var managerIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, activityModel.WfmBuId).ConfigureAwait(false);
            if (managerIds?.Count > 0)
            {
                var manager = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, managerIds[0]).ConfigureAwait(false);

                // get the current set of open shifts from WFM.
                shifts = await _wfmDataService.ListWeekOpenShiftsAsync(activityModel.TeamId, activityModel.WfmBuId, activityModel.StartDate, activityModel.TimeZoneInfoId, manager).ConfigureAwait(false);
            }

            return shifts;
        }

        protected override async Task SetTeamsIdsAsync(DeltaModel<ShiftModel> delta, CacheModel<ShiftModel> savedRecords, TeamActivityModel activityModel, ILogger log)
        {
            var allRecords = delta.All;

            // set job & department name
            var jobLookup = BuildJobLookup(savedRecords.Tracked);
            await SetJobAndDepartmentNameAsync(allRecords, jobLookup, activityModel, log).ConfigureAwait(false);

            // set teams schedule group (N.B this must be set after jobs)
            await SetTeamsSchedulingGroupIdAsync(allRecords.Where(s => string.IsNullOrEmpty(s.TeamsSchedulingGroupId)), activityModel, log).ConfigureAwait(false);
        }
    }
}
