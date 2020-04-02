using JdaTeams.Connector.Functions.Activities;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class InitializeOrchestrator
    {
        private readonly InitializeOrchestratorOptions _options;

        public InitializeOrchestrator(InitializeOrchestratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(InitializeOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();

            await context.CallActivityWithRetryAsync(nameof(ScheduleActivity), _options.AsRetryOptions(), teamModel);

            if (_options.ClearScheduleEnabled)
            {
                await context.CallSubOrchestratorAsync(nameof(ClearScheduleOrchestrator), teamModel);
            }
            else
            {
                await context.CallActivityWithRetryAsync(nameof(ClearCacheActivity), _options.AsRetryOptions(), new ClearScheduleModel { TeamId = teamModel.TeamId });
            }
        }
    }
}
