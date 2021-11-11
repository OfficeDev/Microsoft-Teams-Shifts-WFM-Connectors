// ---------------------------------------------------------------------------
// <copyright file="SenderSwapRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class SenderSwapRequestHandler : SwapRequestHandler
    {
        public SenderSwapRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (base.CanHandleChangeRequest(changeRequest, out ChangeItemRequest itemRequest))
            {
                // a sender swap request is identified by the fact that the method is a POST
                if (itemRequest.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    changeItemRequest = itemRequest;
                }
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            log.LogTrace($"{nameof(SenderSwapRequestHandler)}:{nameof(HandleRequest)}:Started");

            var swapRequest = await ReadRequestObjectAsync<SwapRequest>(changeItemRequest, teamId).ConfigureAwait(false);
            if (swapRequest == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.SwapRequestNotFound, _stringLocalizer[ErrorCodes.SwapRequestNotFound]);
            }

            // are we already processing a request, if so block premature retries
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            if (changeData.SenderStatus == ChangeData.RequestStatus.InProgress)
            {
                changeData = await WaitAndReadChangeDataAsync(swapRequest).ConfigureAwait(false);
                if (changeData.SenderStatus == ChangeData.RequestStatus.InProgress)
                {
                    return new ChangeErrorResult(changeResponse, ErrorCodes.RequestInProgress, _stringLocalizer[ErrorCodes.RequestInProgress], HttpStatusCode.Processing);
                }
            }

            if (changeData.SenderStatus == ChangeData.RequestStatus.Complete)
            {
                // we have already processed this request and it is complete so just return the
                // result we received the first time

                // before doing so, however, because Teams creates a new swap request ID each time,
                // get the previous version of the swap request and update the targetid's of this
                // new request and save it and delete the old one
                var oldSwapRequest = await _requestCacheService.LoadRequestAsync<SwapRequest>(teamId, changeData.SwapRequestId).ConfigureAwait(false);
                swapRequest.FillTargetIds(oldSwapRequest);
                await _requestCacheService.DeleteRequestAsync(teamId, changeData.SwapRequestId).ConfigureAwait(false);

                await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);

                if (changeData.SenderResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    return new ChangeSuccessResult(changeResponse);
                }

                return new ChangeErrorResult(changeResponse, changeItemRequest, changeData.SenderResult.ErrorCode, changeData.SenderResult.ErrorMessage);
            }

            await MapSwapRequestIdentitiesAsync(swapRequest, teamId, log).ConfigureAwait(false);
            if (string.IsNullOrEmpty(swapRequest.TargetSenderShiftId))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.SenderShiftNotFound, _stringLocalizer[ErrorCodes.SenderShiftNotFound]);
            }
            else if (string.IsNullOrEmpty(swapRequest.TargetSenderLoginName))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }
            else if (string.IsNullOrEmpty(swapRequest.TargetRecipientShiftId))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.RecipientShiftNotFound, _stringLocalizer[ErrorCodes.RecipientShiftNotFound]);
            }
            else if (string.IsNullOrEmpty(swapRequest.TargetRecipientLoginName))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            // set the request in progress and store the swap request id in cache in case this call
            // times out and Teams makes a second request
            changeData.SenderStatus = ChangeData.RequestStatus.InProgress;
            // clear other statuses to avoid blocking subsequent requests for the same two shifts
            ResetChangeDataStatuses(changeData);
            changeData.SwapRequestId = changeItemRequest.Id;
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmSwapRequest = swapRequest.AsWfmSwapRequest();
            wfmSwapRequest.BuId = connectionModel.WfmBuId;

            var wfmResponse = await _wfmActionService.CreateShiftSwapRequestAsync(wfmSwapRequest, log).ConfigureAwait(false);
            var actionResult = WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);

            if (wfmResponse.Success)
            {
                swapRequest.TargetSwapRequestId = wfmSwapRequest.SwapRequestId;
                await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);
                await SaveChangeResultAsync(swapRequest).ConfigureAwait(false);
            }
            else
            {
                await SaveChangeResultAsync(swapRequest, HttpStatusCode.BadRequest, wfmResponse.Error.Code, wfmResponse.Error.Message).ConfigureAwait(false);
            }

            // flag that the swap request creation has now finished
            changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            changeData.SenderStatus = ChangeData.RequestStatus.Complete;
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            return actionResult;
        }

        protected override async Task SaveChangeResultAsync(SwapRequest swapRequest, HttpStatusCode statusCode = HttpStatusCode.OK, string errorCode = "", string errorMessage = "")
        {
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            // only update the result if there isn't an existing one
            if (changeData.SenderResult == null)
            {
                changeData.SenderResult = new ChangeResultModel
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
            changeData.RecipientStatus = ChangeData.RequestStatus.NotStarted;
            changeData.RecipientResult = null;
            changeData.ManagerStatus = ChangeData.RequestStatus.NotStarted;
            changeData.ManagerResult = null;
        }

        private ShiftModel FindShift(CacheModel<ShiftModel>[] cacheModels, string shiftId)
        {
            return cacheModels
                .SelectMany(c => c.Tracked)
                .FirstOrDefault(s => s.TeamsShiftId == shiftId);
        }

        private async Task MapSwapRequestIdentitiesAsync(SwapRequest swapRequest, string teamId, ILogger log)
        {
            log.LogTrace($"{nameof(SenderSwapRequestHandler)}:{nameof(MapSwapRequestIdentitiesAsync)}:Started");

            var loadScheduleTasks = DateTime.UtcNow
                .Range(_teamOptions.PastWeeks, _teamOptions.FutureWeeks, _teamOptions.StartDayOfWeek)
                .Select(w => _scheduleCacheService.LoadScheduleAsync(teamId, w));

            var cacheModels = await Task.WhenAll(loadScheduleTasks).ConfigureAwait(false);
            var fromShift = FindShift(cacheModels, swapRequest.SenderShiftId);
            var toShift = FindShift(cacheModels, swapRequest.RecipientShiftId);

            if (fromShift != null)
            {
                swapRequest.TargetSenderShiftId = fromShift.WfmShiftId;
                swapRequest.TargetSenderUserId = fromShift.WfmEmployeeId;
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapRequest.SenderUserId).ConfigureAwait(false);
                if (employee != null)
                {
                    swapRequest.TargetSenderLoginName = GetTargetLoginName(employee.WfmLoginName);
                }
            }

            if (toShift != null)
            {
                swapRequest.TargetRecipientShiftId = toShift.WfmShiftId;
                swapRequest.TargetRecipientUserId = toShift.WfmEmployeeId;
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapRequest.RecipientUserId).ConfigureAwait(false);
                if (employee != null)
                {
                    swapRequest.TargetRecipientLoginName = GetTargetLoginName(employee.WfmLoginName);
                }
            }
        }
    }
}
