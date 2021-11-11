// ---------------------------------------------------------------------------
// <copyright file="TimeOffWeekActivity.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Helpers;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class TimeOffWeekActivity : DeltaActivity<TimeOffModel>
    {
        private readonly ITimeOffCacheService _timeOffCacheService;

        public TimeOffWeekActivity(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<TimeOffModel> deltaService, ICacheService cacheService, ITimeOffCacheService timeOffCacheService)
            : base(options, wfmDataService, teamsService, deltaService, cacheService)
        {
            _timeOffCacheService = timeOffCacheService ?? throw new ArgumentNullException(nameof(timeOffCacheService));
        }

        [FunctionName(nameof(TimeOffWeekActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] WeekModel weekModel, ILogger log)
        {
            var activityModel = new TeamActivityModel
            {
                TeamId = weekModel.TeamId,
                DateValue = weekModel.StartDate.AsDateString(),
                ActivityType = "TimeOff",
                WfmBuId = weekModel.WfmBuId,
                StartDate = weekModel.StartDate,
                TimeZoneInfoId = weekModel.TimeZoneInfoId
            };

            return await RunDeltaActivity(activityModel, log).ConfigureAwait(false);
        }

        protected override async Task ApplyDeltaAsync(TeamActivityModel activityModel, DeltaModel<TimeOffModel> delta, ILogger log)
        {
            await UpdateDestinationAsync(nameof(delta.Created), activityModel, delta, delta.Created, _teamsService.CreateTimeOffAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Updated), activityModel, delta, delta.Updated, _teamsService.UpdateTimeOffAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Deleted), activityModel, delta, delta.Deleted, _teamsService.DeleteTimeOffAsync, log).ConfigureAwait(false);
        }

        protected override async Task<CacheModel<TimeOffModel>> GetSavedRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            return await _timeOffCacheService.LoadTimeOffAsync(activityModel.TeamId, activityModel.StartDate).ConfigureAwait(false);
        }

        protected override async Task<List<TimeOffModel>> GetSourceRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            List<string> wfmEmployeeIds = await CacheHelper.GetWfmEmployeeIdListAsync(_cacheService, activityModel.TeamId).ConfigureAwait(false);

            // get the current set of time off records from the WFM provider for all the employees
            // for the week
            return await _wfmDataService.ListWeekTimeOffAsync(activityModel.TeamId, wfmEmployeeIds, activityModel.StartDate, activityModel.TimeZoneInfoId);
        }

        protected override void LogRecordError(Exception ex, TeamActivityModel activityModel, string operation, TimeOffModel record, ILogger log)
        {
            log.LogTimeOffError(ex, activityModel, operation, record);
        }

        protected override void LogRecordSkipped(TeamActivityModel activityModel, string operation, TimeOffModel record, ILogger log)
        {
            log.LogTimeOffSkipped(activityModel, operation, record);
        }

        protected override async Task SaveRecordsAsync(TeamActivityModel activityModel, CacheModel<TimeOffModel> savedRecords)
        {
            await _timeOffCacheService.SaveTimeOffAsync(activityModel.TeamId, activityModel.StartDate, savedRecords).ConfigureAwait(false);
        }

        protected override async Task SetTeamsIdsAsync(DeltaModel<TimeOffModel> delta, CacheModel<TimeOffModel> savedRecords, TeamActivityModel activityModel, ILogger log)
        {
            var allTimeOff = delta.All;

            // set time off reasons
            await SetTimeOffReasonAsync(allTimeOff, savedRecords.Tracked, activityModel, log).ConfigureAwait(false);

            // set teams employee
            await SetTeamsEmployeeIdsAsync(allTimeOff.Where(s => string.IsNullOrEmpty(s.TeamsEmployeeId)), activityModel.TeamId).ConfigureAwait(false);
        }

        private async Task SetTimeOffReasonAsync(List<TimeOffModel> timeOffRecords, List<TimeOffModel> savedTimeOffRecords, TeamActivityModel activityModel, ILogger log)
        {
            var timeOffRecsToUpdate = timeOffRecords.Where(t => string.IsNullOrEmpty(t.TeamsTimeOffReasonId));

            // use the saved time off records to get the teams time off reason
            foreach (var timeOffRecToUpdate in timeOffRecsToUpdate)
            {
                timeOffRecToUpdate.TeamsTimeOffReasonId = savedTimeOffRecords.FirstOrDefault(s => s.WfmTimeOffTypeId == timeOffRecToUpdate.WfmTimeOffTypeId)?.TeamsTimeOffReasonId;
            }

            // get the remaining unmapped time off records, if any
            timeOffRecsToUpdate = timeOffRecsToUpdate.Where(t => string.IsNullOrEmpty(t.TeamsTimeOffReasonId)).ToList();

            if (!timeOffRecsToUpdate.Any())
            {
                return;
            }

            // get the set of all time off reasons from Teams
            var timeOffReasons = await _teamsService.ListTimeOffReasonsAsync(activityModel.TeamId).ConfigureAwait(false);
            // process the distinct set of reasons from the remaining records to update
            foreach (var wfmTimeOff in timeOffRecsToUpdate.Distinct(t => t.WfmTimeOffTypeId))
            {
                var timeOffReason = timeOffReasons.Find(t => t.Reason.Equals(wfmTimeOff.WfmTimeOffReason, StringComparison.OrdinalIgnoreCase));
                if (timeOffReason == null)
                {
                    // this reason does not exist in Teams, so create it
                    try
                    {
                        timeOffReason = new TimeOffReasonModel
                        {
                            Reason = wfmTimeOff.WfmTimeOffReason
                        };
                        timeOffReason = await _teamsService.CreateTimeOffReasonAsync(activityModel.TeamId, timeOffReason).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // we failed to create this time off reason so skip this one and continue
                        // with the next
                        log.LogTimeOffReasonError(ex, activityModel, timeOffReason);
                        continue;
                    }
                }

                // update all time off records with this reason with the teams time off reason id
                foreach (var timeOffRec in timeOffRecsToUpdate.Where(t => t.WfmTimeOffTypeId == wfmTimeOff.WfmTimeOffTypeId))
                {
                    timeOffRec.TeamsTimeOffReasonId = timeOffReason.TeamsTimeOffReasonId;
                }
            }
        }
    }
}
