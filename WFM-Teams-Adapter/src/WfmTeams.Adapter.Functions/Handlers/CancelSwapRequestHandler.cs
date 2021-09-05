// ---------------------------------------------------------------------------
// <copyright file="CancelSwapRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Services;

    public class CancelSwapRequestHandler : SwapRequestHandler
    {
        public CancelSwapRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (base.CanHandleChangeRequest(changeRequest, out ChangeItemRequest itemRequest))
            {
                if (itemRequest.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
                {
                    changeItemRequest = itemRequest;
                }
                else if (itemRequest.Body != null)
                {
                    var swapRequest = itemRequest.Body.ToObject<SwapRequest>();

                    if (swapRequest.AssignedTo.Equals(ChangeRequestAssignedTo.System, StringComparison.OrdinalIgnoreCase)
                        && swapRequest.State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase))
                    {
                        changeItemRequest = itemRequest;
                    }
                }
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            var swapRequest = await ReadRequestObjectAsync<SwapRequest>(changeItemRequest, teamId).ConfigureAwait(false);
            if (swapRequest == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.SwapRequestNotFound, _stringLocalizer[ErrorCodes.SwapRequestNotFound]);
            }

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmSwapRequest = swapRequest.AsWfmSwapRequest();
            wfmSwapRequest.BuId = connectionModel.WfmBuId;

            var wfmResponse = await _wfmActionService.CancelShiftSwapRequestAsync(wfmSwapRequest, log).ConfigureAwait(false);
            var actionResult = WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);

            if (wfmResponse.Success)
            {
                await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);
            }

            // whether we successfully cancelled the swap request or not, ensure we clear the cache data
            await DeleteChangeDataAsync(swapRequest).ConfigureAwait(false);

            return actionResult;
        }

        protected override Task SaveChangeResultAsync(SwapRequest swapRequest, HttpStatusCode statusCode = HttpStatusCode.OK, string errorCode = "", string errorMessage = "")
        {
            // currently nothing to do
            return Task.CompletedTask;
        }
    }
}
