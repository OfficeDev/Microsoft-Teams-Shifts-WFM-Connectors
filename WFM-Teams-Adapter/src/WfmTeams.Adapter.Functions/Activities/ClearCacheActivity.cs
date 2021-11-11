// ---------------------------------------------------------------------------
// <copyright file="ClearCacheActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Services;

    public class ClearCacheActivity
    {
        private readonly FeatureOptions _featureOptions;

        private readonly TeamOrchestratorOptions _options;

        private readonly IScheduleCacheService _scheduleCacheService;

        private readonly ITimeOffCacheService _timeOffCacheService;

        public ClearCacheActivity(FeatureOptions featureOptions, TeamOrchestratorOptions options, IScheduleCacheService scheduleCacheService, ITimeOffCacheService timeOffCacheService)
        {
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _timeOffCacheService = timeOffCacheService ?? throw new ArgumentNullException(nameof(timeOffCacheService));
        }

        [FunctionName(nameof(ClearCacheActivity))]
        public async Task Run([ActivityTrigger] ClearScheduleModel clearScheduleModel, ILogger log)
        {
            var weeksRange = clearScheduleModel.StartDate
                .Range(clearScheduleModel.EndDate, _options.StartDayOfWeek);

            foreach (var week in weeksRange)
            {
                if (clearScheduleModel.ClearShifts)
                {
                    await _scheduleCacheService.DeleteScheduleAsync(clearScheduleModel.TeamId, week).ConfigureAwait(false);
                }

                if (_featureOptions.EnableOpenShiftSync && clearScheduleModel.ClearOpenShifts)
                {
                    await _scheduleCacheService.DeleteScheduleAsync(clearScheduleModel.TeamId + ApplicationConstants.OpenShiftsSuffix, week).ConfigureAwait(false);
                }

                if (_featureOptions.EnableTimeOffSync && clearScheduleModel.ClearTimeOff)
                {
                    await _timeOffCacheService.DeleteTimeOffAsync(clearScheduleModel.TeamId, week).ConfigureAwait(false);
                }
            }
        }
    }
}
