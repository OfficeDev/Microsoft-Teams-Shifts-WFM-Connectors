// ---------------------------------------------------------------------------
// <copyright file="TimeOffOrchestrator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Orchestrators
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Activities;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;

    public class TimeOffOrchestrator
    {
        private readonly FeatureOptions _featureOptions;
        private readonly TeamOrchestratorOptions _options;

        public TimeOffOrchestrator(TeamOrchestratorOptions options, FeatureOptions featureOptions)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
        }

        public static string InstanceIdPattern => "{0}-TimeOff";

        [FunctionName(nameof(TimeOffOrchestrator))]
        public async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            if (!_featureOptions.EnableTimeOffSync)
            {
                return;
            }

            var teamModel = context.GetInput<TeamModel>();

            var weeks = context.CurrentUtcDateTime.Date
                .Range(_options.PastWeeks, _options.FutureWeeks, _options.StartDayOfWeek);

            // give the wfm provider connector the opportunity to prepare the data for the sync if necessary
            await context.CallActivityAsync(nameof(PrepareSyncActivity), new PrepareSyncModel
            {
                Type = SyncType.TimeOff,
                TeamId = teamModel.TeamId,
                WfmId = teamModel.WfmBuId,
                FirstWeekStartDate = weeks.Min(),
                LastWeekStartDate = weeks.Max(),
                TimeZoneInfoId = teamModel.TimeZoneInfoId
            });

            var weekModels = weeks
                .Select(startDate => new WeekModel
                {
                    StartDate = startDate,
                    WfmBuId = teamModel.WfmBuId,
                    TeamId = teamModel.TeamId
                });

            var weekTasks = weekModels
                .Select(weekModel => context.CallSubOrchestratorAsync<bool>(nameof(TimeOffWeekOrchestrator), weekModel));

            var allTasks = Task.WhenAll(weekTasks);

            bool changesProcessed = false;
            try
            {
                var results = await allTasks;
                changesProcessed = results.Any(b => b);
            }
            finally
            {
                // always commit what we have successfully applied to Teams
                if (_options.DraftShiftsEnabled && changesProcessed)
                {
                    var shareModel = new ShareModel
                    {
                        TeamId = teamModel.TeamId,
                        StartDate = weekModels.Min(w => w.StartDate),
                        EndDate = weekModels.Max(w => w.StartDate).AddWeek()
                    };
                    await context.CallActivityAsync(nameof(ShareActivity), shareModel);
                }
            }

            if (allTasks.Status == TaskStatus.Faulted)
            {
                log.LogAggregateOrchestrationError(allTasks.Exception, teamModel, nameof(TimeOffOrchestrator));
            }
        }
    }
}
