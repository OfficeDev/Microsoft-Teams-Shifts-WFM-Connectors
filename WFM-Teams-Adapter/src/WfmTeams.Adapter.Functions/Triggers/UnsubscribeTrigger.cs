// ---------------------------------------------------------------------------
// <copyright file="UnsubscribeTrigger.cs" company="Microsoft">
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
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Options;
    using WfmTeams.Adapter.Services;

    public class UnsubscribeTrigger
    {
        private readonly ConnectorOptions _connectorOptions;

        private readonly FeatureOptions _options;

        private readonly IScheduleConnectorService _scheduleConnectorService;

        private readonly ISecretsService _secretsService;

        public UnsubscribeTrigger(FeatureOptions options, ConnectorOptions connectorOptions, ISecretsService secretsService, IScheduleConnectorService scheduleConnectorService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _connectorOptions = connectorOptions ?? throw new ArgumentNullException(nameof(connectorOptions));
        }

        [FunctionName(nameof(UnsubscribeTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "unsubscribe/{teamId}")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            try
            {
                var connection = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }

            // ensure that in the very brief period of time before the connection is deleted that
            // the orchestrators are not started
            await _scheduleConnectorService.UpdateEnabledAsync(teamId, false).ConfigureAwait(false);

            // and that any running instances are terminated
            await StopTrigger.StopRunningOrchestratorsAsync(teamId, starter).ConfigureAwait(false);

            // finally, delete the connection
            await _scheduleConnectorService.DeleteConnectionAsync(teamId).ConfigureAwait(false);

            log.LogUnsubscribeTeam(teamId);

            return new OkResult();
        }
    }
}
