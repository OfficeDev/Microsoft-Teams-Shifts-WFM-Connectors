using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.MicrosoftGraph.Options;
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
    public class ConfigTrigger
    {
        private readonly MicrosoftGraphOptions _microsoftGraphOptions;
        private readonly JdaPersonaOptions _jdaPersonaOptions;
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public ConfigTrigger(MicrosoftGraphOptions microsoftGraphOptions, JdaPersonaOptions jdaPersonaOptions, IScheduleConnectorService scheduleConnectorService)
        {
            _microsoftGraphOptions = microsoftGraphOptions ?? throw new ArgumentNullException(nameof(microsoftGraphOptions));
            _jdaPersonaOptions = jdaPersonaOptions ?? throw new ArgumentNullException(nameof(jdaPersonaOptions));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(ConfigTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/{teamId?}")] HttpRequest req,
            string teamId,
            ILogger log)
        {
            var configModel = new ConfigModel
            {
                AuthorizeUrl = _microsoftGraphOptions.AuthorizeUrl,
                ClientId = _microsoftGraphOptions.ClientId,
                Scope = _microsoftGraphOptions.Scope,
                JdaBaseAddress = _jdaPersonaOptions.JdaBaseAddress,
                ShiftsAppUrl = _microsoftGraphOptions.ShiftsAppUrl
            };

            if (teamId != null)
            {
                try
                {
                    var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId);

                    configModel.Connected = true;
                }
                catch (KeyNotFoundException)
                {
                    configModel.Connected = false;
                }
            }

            return new JsonResult(configModel);
        }
    }
}