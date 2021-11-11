﻿// ---------------------------------------------------------------------------
// <copyright file="UsersChangeRequestTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Handlers;
    using WfmTeams.Adapter.Functions.Options;

    public class UsersChangeRequestTrigger : ChangeRequestTrigger
    {
        public UsersChangeRequestTrigger(WorkforceIntegrationOptions workforceIntegrationOptions, IEnumerable<IChangeRequestHandler> handlers, IStringLocalizer<ChangeRequestTrigger> stringLocalizer)
            : base(workforceIntegrationOptions, stringLocalizer)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            // filter the handlers to those relating to User change requests only
            _handlers = handlers.Where(h => h.ChangeHandlerType == HandlerType.Users);
        }

        [FunctionName(nameof(UsersChangeRequestTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "change/v{version:int}/users/{userId}/update")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            int version,
            string userId,
            ILogger log)
        {
            log.LogTrace("UsersChangeRequestTrigger:Started at {startedTime}, User: {userId}, Version: {version}", DateTime.UtcNow.ToString("o"), userId, version);

            return await RunTrigger(req, starter, version, userId, log).ConfigureAwait(false);
        }
    }
}
