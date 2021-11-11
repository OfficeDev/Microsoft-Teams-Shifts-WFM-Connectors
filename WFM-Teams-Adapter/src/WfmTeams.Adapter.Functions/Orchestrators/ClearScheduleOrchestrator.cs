// ---------------------------------------------------------------------------
// <copyright file="ClearScheduleOrchestrator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Orchestrators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Activities;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;

    public class ClearScheduleOrchestrator
    {
        private readonly FeatureOptions _featureOptions;

        private readonly TeamOrchestratorOptions _options;

        public ClearScheduleOrchestrator(FeatureOptions featureOptions, TeamOrchestratorOptions options)
        {
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public static string InstanceId(string teamId)
        {
            return teamId + "-ClearSchedule";
        }

        [FunctionName(nameof(ClearScheduleOrchestrator))]
        public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var clearScheduleModel = context.GetInput<ClearScheduleModel>();

            if (!context.IsReplaying)
            {
                log.LogClearStart(clearScheduleModel, "Schedule");
            }

            var tasks = new List<Task>();
            // We split date range by 24 hour periods to ensure that the activity does not timeout
            var dayModels = Enumerable.Range(0, clearScheduleModel.UtcEndDate.Subtract(clearScheduleModel.UtcStartDate).Days)
                .Select(offset => new ClearScheduleModel
                {
                    StartDate = clearScheduleModel.StartDate,
                    EndDate = clearScheduleModel.EndDate,
                    UtcStartDate = clearScheduleModel.UtcStartDate.AddDays(offset),
                    UtcEndDate = clearScheduleModel.UtcStartDate.AddDays(offset).AddHours(23).AddMinutes(59),
                    TeamId = clearScheduleModel.TeamId
                });
            if (clearScheduleModel.ClearShifts)
            {
                tasks = dayModels
                    .Select(model => context.CallActivityAsync(nameof(ClearShiftsActivity), model))
                    .ToList();
            }

            if (_featureOptions.EnableOpenShiftSync && clearScheduleModel.ClearOpenShifts)
            {
                tasks.AddRange(dayModels
                    .Select(model => context.CallActivityAsync(nameof(ClearOpenShiftsActivity), model)));
            }

            if (_featureOptions.EnableTimeOffSync && clearScheduleModel.ClearTimeOff)
            {
                tasks.AddRange(dayModels
                    .Select(model => context.CallActivityAsync(nameof(ClearTimeOffActivity), model)));
            }

            await Task.WhenAll(tasks);

            // at this stage, we should have deleted all the shifts/open shifts for each of the days
            // in the period, apart from those that span midnight on any day, so we need to execute
            // a final ClearShiftsDayOrchestrator & ClearOpenShiftsDayOrchestrator for the full date
            // range plus 24 hours in order to include those remaining shifts
            clearScheduleModel.QueryEndDate = clearScheduleModel.UtcEndDate.AddHours(24);
            if (clearScheduleModel.ClearShifts)
            {
                await context.CallActivityAsync(nameof(ClearShiftsActivity), clearScheduleModel);
            }
            if (_featureOptions.EnableOpenShiftSync && clearScheduleModel.ClearOpenShifts)
            {
                await context.CallActivityAsync(nameof(ClearOpenShiftsActivity), clearScheduleModel);
            }
            if (_featureOptions.EnableTimeOffSync && clearScheduleModel.ClearTimeOff)
            {
                await context.CallActivityAsync(nameof(ClearTimeOffActivity), clearScheduleModel);
            }

            // because we are using staged deletes for shifts, open shifts and time off, these need
            // to be shared in order to publish them
            var shareModel = new ShareModel
            {
                TeamId = clearScheduleModel.TeamId,
                StartDate = clearScheduleModel.UtcStartDate,
                EndDate = clearScheduleModel.UtcEndDate
            };
            await context.CallActivityAsync(nameof(ShareActivity), shareModel);

            if (clearScheduleModel.ClearSchedulingGroups)
            {
                await context.CallActivityAsync(nameof(ClearSchedulingGroupsActivity), clearScheduleModel.TeamId);
            }

            await context.CallActivityAsync(nameof(ClearCacheActivity), clearScheduleModel);
        }
    }
}
