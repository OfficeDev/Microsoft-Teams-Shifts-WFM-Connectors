using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Orchestrators;
using JdaTeams.Connector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class RestartTrigger
    {
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public RestartTrigger(IScheduleConnectorService scheduleConnectorService)
        {
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(RestartTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "restart")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            var connections = await _scheduleConnectorService.ListConnectionsAsync();

            foreach (var connectionModel in connections)
            {
                var teamModel = TeamModel.FromConnection(connectionModel);

                await starter.TryStartSingletonAsync(nameof(TeamOrchestrator), teamModel.TeamId, teamModel);
            }

            return new OkResult();
        }
    }
}
