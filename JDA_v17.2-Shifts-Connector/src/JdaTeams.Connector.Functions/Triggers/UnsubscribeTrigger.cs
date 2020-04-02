using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class UnsubscribeTrigger
    {
        private readonly ISecretsService _secretsService;
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public UnsubscribeTrigger(ISecretsService secretsService, IScheduleConnectorService scheduleConnectorService)
        {
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(UnsubscribeTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "unsubscribe/{teamId}")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            try
            {
                var connection = await _scheduleConnectorService.GetConnectionAsync(teamId);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }

            var tasks = new Task[]
            {
                starter.TerminateAsync(teamId, nameof(UnsubscribeTrigger)),
                _secretsService.DeleteCredentialsAsync(teamId),
                _secretsService.DeleteTokenAsync(teamId),
                _scheduleConnectorService.DeleteConnectionAsync(teamId)
            };

            await Task.WhenAll(tasks);

            return new OkResult();
        }
    }
}
