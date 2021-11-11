// ---------------------------------------------------------------------------
// <copyright file="AvailabilityOrchestrator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Orchestrators
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Activities;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;

    public class AvailabilityOrchestrator
    {
        private readonly FeatureOptions _featureOptions;
        private readonly TeamOrchestratorOptions _options;

        public AvailabilityOrchestrator(TeamOrchestratorOptions options, FeatureOptions featureOptions)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
        }

        public static string InstanceIdPattern => "{0}-Availability";

        [FunctionName(nameof(AvailabilityOrchestrator))]
        public async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();

            if (!_featureOptions.EnableAvailabilitySync)
            {
                return;
            }

            try
            {
                // give the wfm provider connector the opportunity to prepare the data for the sync
                // if necessary
                await context.CallActivityAsync(nameof(PrepareSyncActivity), new PrepareSyncModel
                {
                    Type = SyncType.Availability,
                    TeamId = teamModel.TeamId,
                    WfmId = teamModel.WfmBuId,
                    TimeZoneInfoId = teamModel.TimeZoneInfoId
                });

                await context.CallActivityAsync(nameof(AvailabilityActivity), teamModel);
            }
            catch (Exception ex)
            {
                log.LogOrchestrationError(ex, teamModel, nameof(AvailabilityOrchestrator));
            }
        }
    }
}
