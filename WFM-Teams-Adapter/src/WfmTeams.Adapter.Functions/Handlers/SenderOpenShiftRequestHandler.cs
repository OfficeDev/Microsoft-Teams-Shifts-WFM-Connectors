// ---------------------------------------------------------------------------
// <copyright file="SenderOpenShiftRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
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
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class SenderOpenShiftRequestHandler : OpenShiftRequestHandler
    {
        public SenderOpenShiftRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (CanHandleChangeRequest(changeRequest, OpenShiftRequestUriTemplate, out ChangeItemRequest itemRequest))
            {
                if (itemRequest.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    changeItemRequest = itemRequest;
                }
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            var openShiftRequest = await ReadRequestObjectAsync<OpenShiftsChangeRequest>(changeItemRequest, teamId).ConfigureAwait(false);
            if (openShiftRequest == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.ChangeRequestNotFound, _stringLocalizer[ErrorCodes.ChangeRequestNotFound]);
            }

            if (!await MapOpenShiftRequestIdentitiesAsync(openShiftRequest).ConfigureAwait(false))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            var openShift = await GetOpenShift(teamId, openShiftRequest.OpenShiftId).ConfigureAwait(false);
            if (openShift == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.NoOpenShiftsFound, _stringLocalizer[ErrorCodes.NoOpenShiftsFound]);
            }

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmOpenShiftRequest = openShiftRequest.AsWfmOpenShiftRequest();
            wfmOpenShiftRequest.BuId = connectionModel.WfmBuId;
            wfmOpenShiftRequest.TimeZoneInfoId = connectionModel.TimeZoneInfoId;
            wfmOpenShiftRequest.WfmOpenShift = openShift;

            var wfmResponse = await _wfmActionService.CreateOpenShiftRequestAsync(wfmOpenShiftRequest, log).ConfigureAwait(false);

            if (wfmResponse.Success)
            {
                await _requestCacheService.SaveRequestAsync(teamId, openShiftRequest.Id, openShiftRequest).ConfigureAwait(false);

                if (_featureOptions.EnableOpenShiftAutoApproval)
                {
                    await AutoApproveRequestAsync(openShiftRequest, teamId, starter).ConfigureAwait(false);
                }

                return new ChangeSuccessResult(changeResponse);
            }

            return new ChangeErrorResult(changeResponse, ErrorCodes.ShiftNotAvailableToUser, _stringLocalizer[ErrorCodes.ShiftNotAvailableToUser]);
        }

        protected override async Task<bool> MapOpenShiftRequestIdentitiesAsync(OpenShiftsChangeRequest openShiftRequest)
        {
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, openShiftRequest.SenderUserId).ConfigureAwait(false);
            if (employee != null)
            {
                openShiftRequest.WfmSenderId = employee.WfmEmployeeId;
                openShiftRequest.WfmSenderLoginName = GetTargetLoginName(employee.WfmLoginName);
                return true;
            }

            return false;
        }

        private async Task AutoApproveRequestAsync(OpenShiftsChangeRequest openShiftRequest, string teamId, IDurableOrchestrationClient starter)
        {
            var deferredActionModel = new DeferredActionModel
            {
                ActionType = DeferredActionModel.DeferredActionType.ApproveOpenShiftRequest,
                DelaySeconds = _teamOptions.DelayedActionSeconds,
                RequestId = openShiftRequest.Id,
                TeamId = teamId
            };
            await starter.StartNewAsync(nameof(DeferredActionOrchestrator), deferredActionModel).ConfigureAwait(false);
        }
    }
}
