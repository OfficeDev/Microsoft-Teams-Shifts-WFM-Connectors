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
    public class StartTrigger
    {
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public StartTrigger(IScheduleConnectorService scheduleConnectorService)
        {
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(StartTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "start/{teamId}")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId);
            var teamModel = TeamModel.FromConnection(connectionModel);

            if (await starter.TryStartSingletonAsync(nameof(TeamOrchestrator), teamModel.TeamId, teamModel))
            {
                return new OkResult();
            }
            else
            {
                return new ConflictResult();
            }
        }
    }
}
