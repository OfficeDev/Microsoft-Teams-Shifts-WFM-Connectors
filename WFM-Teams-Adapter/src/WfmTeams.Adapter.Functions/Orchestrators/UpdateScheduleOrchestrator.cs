// ---------------------------------------------------------------------------
// <copyright file="UpdateScheduleOrchestrator.cs" company="Microsoft">
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

    public class UpdateScheduleOrchestrator
    {
        private readonly WorkforceIntegrationOptions _options;

        public UpdateScheduleOrchestrator(WorkforceIntegrationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public static string InstanceId => "9434808b-83e6-46f7-ae80-854c2cb000fa";

        [FunctionName(nameof(UpdateScheduleOrchestrator))]
        public async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var updateScheduleModel = context.GetInput<UpdateScheduleModel>();

            log.LogUpdateSchedule(updateScheduleModel, nameof(UpdateScheduleOrchestrator));

            foreach (var teamId in updateScheduleModel.TeamIds.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                var teamModel = new TeamModel
                {
                    TeamId = teamId
                };
                await context.CallActivityAsync(nameof(ScheduleActivity), teamModel);
            }
        }
    }
}
