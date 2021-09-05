// ---------------------------------------------------------------------------
// <copyright file="ConnectTrigger.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;

    public class ConnectTrigger
    {
        private readonly WorkforceIntegrationOptions _options;

        public ConnectTrigger(WorkforceIntegrationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(ConnectTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "change/v{version:int}/connect")] HttpRequest httpRequest,
            int version,
            ILogger log)
        {
            if (version != _options.ApiVersion)
            {
                log.LogError("Unsupported API version: {apiVersion}", version);
                return new BadRequestResult();
            }

            try
            {
                var connectModel = await httpRequest.ReadAsObjectAsync<ConnectModel>(_options.WorkforceIntegrationSecret).ConfigureAwait(false);

                if (string.IsNullOrEmpty(connectModel.TenantId) || !connectModel.TenantId.Equals(_options.TenantId, StringComparison.OrdinalIgnoreCase))
                {
                    log.LogError("Unsupported tenant ID: {tenantId}", connectModel.TenantId);
                    return new BadRequestResult();
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error handling connect request from Teams");
                return new BadRequestResult();
            }
        }
    }
}
