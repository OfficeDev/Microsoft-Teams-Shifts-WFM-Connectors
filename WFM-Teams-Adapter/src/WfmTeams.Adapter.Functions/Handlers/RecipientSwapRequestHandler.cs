// ---------------------------------------------------------------------------
// <copyright file="RecipientSwapRequestHandler.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Enums;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Services;

    public class RecipientSwapRequestHandler : SwapRequestHandler
    {
        public RecipientSwapRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (base.CanHandleChangeRequest(changeRequest, out ChangeItemRequest itemRequest))
            {
                if (itemRequest.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    // this is an approval, but is it a recipient approval
                    var swapRequest = itemRequest.Body.ToObject<SwapRequest>();
                    var state = swapRequest.EvaluateState(itemRequest.Method);
                    if (state == ChangeRequestState.RecipientApproved || state == ChangeRequestState.RecipientDeclined)
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

            // are we already processing a request, if so block premature retries
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            if (changeData.RecipientStatus == ChangeData.RequestStatus.InProgress)
            {
                changeData = await WaitAndReadChangeDataAsync(swapRequest).ConfigureAwait(false);
                if (changeData.RecipientStatus == ChangeData.RequestStatus.InProgress)
                {
                    return new ChangeErrorResult(changeResponse, ErrorCodes.RequestInProgress, _stringLocalizer[ErrorCodes.RequestInProgress], HttpStatusCode.Processing);
                }
            }

            if (changeData.RecipientStatus == ChangeData.RequestStatus.Complete)
            {
                if (changeData.RecipientResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    return new ChangeSuccessResult(changeResponse);
                }

                return new ChangeErrorResult(changeResponse, changeItemRequest, changeData.RecipientResult.ErrorCode, changeData.RecipientResult.ErrorMessage);
            }

            changeData.RecipientStatus = ChangeData.RequestStatus.InProgress;
            // clear other statuses to avoid blocking subsequent requests for the same two shifts
            ResetChangeDataStatuses(changeData);
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmSwapRequest = swapRequest.AsWfmSwapRequest();
            wfmSwapRequest.BuId = connectionModel.WfmBuId;

            var approve = swapRequest.EvaluateState(changeItemRequest.Method) == ChangeRequestState.RecipientApproved;
            var wfmResponse = await _wfmActionService.RecipientApproveShiftSwapRequestAsync(wfmSwapRequest, approve, log).ConfigureAwait(false);
            var actionResult = WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);

            if (wfmResponse.Success)
            {
                await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);
                await SaveChangeResultAsync(swapRequest).ConfigureAwait(false);
            }
            else
            {
                await SaveChangeResultAsync(swapRequest, HttpStatusCode.BadRequest, wfmResponse.Error.Code, wfmResponse.Error.Message).ConfigureAwait(false);
            }

            await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);

            // flag that the recipient approval has now finished
            changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            changeData.RecipientStatus = ChangeData.RequestStatus.Complete;
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            // if the auto manager approval feature is enabled, issue it now
            if (actionResult is ChangeSuccessResult && _featureOptions.EnableShiftSwapAutoApproval)
            {
                var deferredActionModel = new DeferredActionModel
                {
                    ActionType = DeferredActionModel.DeferredActionType.ApproveSwapShiftsRequest,
                    DelaySeconds = _teamOptions.DelayedActionSeconds,
                    RequestId = swapRequest.Id,
                    TeamId = teamId,
                    Message = _stringLocalizer.GetString("ApproveSwapShifts")
                };
                await starter.StartNewAsync(nameof(DeferredActionOrchestrator), deferredActionModel).ConfigureAwait(false);
            }

            return actionResult;
        }

        protected override async Task SaveChangeResultAsync(SwapRequest swapRequest, HttpStatusCode statusCode = HttpStatusCode.OK, string errorCode = "", string errorMessage = "")
        {
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            // only update the result if there isn't an existing one
            if (changeData.RecipientResult == null)
            {
                changeData.RecipientResult = new ChangeResultModel
                {
                    StatusCode = (int)statusCode,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                };
                await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);
            }
        }

        private void ResetChangeDataStatuses(ChangeData changeData)
        {
            changeData.SenderStatus = ChangeData.RequestStatus.NotStarted;
            changeData.SenderResult = null;
            changeData.ManagerStatus = ChangeData.RequestStatus.NotStarted;
            changeData.ManagerResult = null;
        }
    }
}
