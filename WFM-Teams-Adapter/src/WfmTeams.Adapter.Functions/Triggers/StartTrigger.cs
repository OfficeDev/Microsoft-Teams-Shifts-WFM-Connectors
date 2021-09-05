// ---------------------------------------------------------------------------
// <copyright file="StartTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Services;

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
            string teamId,
            ILogger log)
        {
            await _scheduleConnectorService.UpdateEnabledAsync(teamId, true).ConfigureAwait(false);
            log.LogEnableOrchestrators(teamId);

            return new OkResult();
        }
    }
}
