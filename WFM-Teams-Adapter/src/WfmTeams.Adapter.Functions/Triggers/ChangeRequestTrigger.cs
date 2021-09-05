// ---------------------------------------------------------------------------
// <copyright file="ChangeRequestTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Handlers;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public abstract class ChangeRequestTrigger
    {
        protected readonly IStringLocalizer<ChangeRequestTrigger> _stringLocalizer;
        protected readonly WorkforceIntegrationOptions _workforceIntegrationOptions;
        protected IEnumerable<IChangeRequestHandler> _handlers;

        protected ChangeRequestTrigger(WorkforceIntegrationOptions workforceIntegrationOptions, IStringLocalizer<ChangeRequestTrigger> stringLocalizer)
        {
            _workforceIntegrationOptions = workforceIntegrationOptions ?? throw new ArgumentNullException(nameof(workforceIntegrationOptions));
            _stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
        }

        protected async Task<IActionResult> RunTrigger(HttpRequest req, IDurableOrchestrationClient starter, int version, string entityId, ILogger log)
        {
            req.ApplyThreadCulture();

            var changeRequest = await req.ReadAsObjectAsync<ChangeRequest>(_workforceIntegrationOptions.WorkforceIntegrationSecret).ConfigureAwait(false);
            ChangeResponse changeResponse = new ChangeResponse(changeRequest);
            if (IsPassThroughRequest(req.Headers))
            {
                // as this change has occurred as a result of a connector action, bypass all further processing
                return new ChangeSuccessResult(changeResponse);
            }

            log.LogChangeRequest(changeRequest, entityId);

            if (!TryValidateChangeRequest(changeRequest, version, entityId, log, out var validationResponse))
            {
                return validationResponse;
            }

            IActionResult actionResult = null;
            try
            {
                bool handled = false;
                foreach (var handler in _handlers)
                {
                    if (handler.CanHandleChangeRequest(changeRequest, out var changeItemRequest))
                    {
                        log.LogTrace("Change Request handled by: {handlerName}", handler.GetType().Name);
                        if (TryValidateChangeItemRequest(changeItemRequest, changeResponse, out validationResponse))
                        {
                            actionResult = await handler.HandleRequest(changeRequest, changeItemRequest, changeResponse, entityId, log, starter).ConfigureAwait(false);
                        }
                        else
                        {
                            actionResult = validationResponse;
                        }
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                {
                    // we have no handler for this.
                    actionResult = new ChangeErrorResult(changeResponse, ErrorCodes.UnsupportedOperation, _stringLocalizer[ErrorCodes.UnsupportedOperation], HttpStatusCode.Forbidden);
                }
            }
            catch (Exception ex)
            {
                log.LogChangeError(ex, entityId);
                actionResult = new ChangeErrorResult(changeResponse, ErrorCodes.InternalError, _stringLocalizer[ErrorCodes.InternalError], HttpStatusCode.InternalServerError);
            }

            log.LogChangeResult(actionResult);

            return actionResult;
        }

        private bool IsPassThroughRequest(IHeaderDictionary headers)
        {
            if (headers?.ContainsKey(ChangeRequest.MSPassthroughRequestHeader) ?? false)
            {
                var headerValue = headers[ChangeRequest.MSPassthroughRequestHeader][0];
                if (headerValue.Equals(ChangeRequest.PassThroughName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryValidateChangeItemRequest(ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, out IActionResult validationResponse)
        {
            validationResponse = null;

            if (changeItemRequest.Headers == null || changeItemRequest.Headers.Count == 0)
            {
                return true;
            }

            if (changeItemRequest.Headers.Expires.HasValue && DateTime.UtcNow > changeItemRequest.Headers.Expires.Value)
            {
                validationResponse = new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.RequestExpired, _stringLocalizer[ErrorCodes.RequestExpired]);
                return false;
            }

            return true;
        }

        private bool TryValidateChangeRequest(ChangeRequest changeRequest, int version, string entityId, ILogger log, out IActionResult validationResponse)
        {
            if (changeRequest.Requests.Count() == 0)
            {
                log.LogError("BadRequest: Request Count 0");
                validationResponse = new BadRequestResult();
                return false;
            }

            if (string.IsNullOrEmpty(entityId))
            {
                log.LogError("BadRequest: Missing EntityId");
                validationResponse = new BadRequestResult();
                return false;
            }

            if (version != _workforceIntegrationOptions.ApiVersion)
            {
                log.LogError("BadRequest: Unsupported API Version");
                validationResponse = new BadRequestResult();
                return false;
            }

            validationResponse = null;
            return true;
        }

        private class HandlerItem
        {
            public IChangeRequestHandler Handler { get; set; }
            public ChangeItemRequest ItemRequest { get; set; }
            public string ItemRequestId { get; set; }
        }
    }
}
