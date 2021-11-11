// ---------------------------------------------------------------------------
// <copyright file="InitializeOrchestrator.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;

    public class InitializeOrchestrator
    {
        private readonly InitializeOrchestratorOptions _options;

        public InitializeOrchestrator(InitializeOrchestratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(InitializeOrchestrator))]
        public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();

            await context.CallActivityAsync(nameof(ScheduleActivity), teamModel);

            if (_options.ClearScheduleEnabled)
            {
                await context.CallSubOrchestratorAsync(nameof(ClearScheduleOrchestrator), teamModel);
            }
        }
    }
}
