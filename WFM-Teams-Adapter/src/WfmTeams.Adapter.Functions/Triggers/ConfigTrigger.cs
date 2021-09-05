// ---------------------------------------------------------------------------
// <copyright file="ConfigTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Services;

    public class ConfigTrigger
    {
        private readonly MicrosoftGraphOptions _microsoftGraphOptions;
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public ConfigTrigger(MicrosoftGraphOptions microsoftGraphOptions, IScheduleConnectorService scheduleConnectorService)
        {
            _microsoftGraphOptions = microsoftGraphOptions ?? throw new ArgumentNullException(nameof(microsoftGraphOptions));
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
                ShiftsAppUrl = _microsoftGraphOptions.ShiftsAppUrl
            };

            if (teamId != null)
            {
                try
                {
                    var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);

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
