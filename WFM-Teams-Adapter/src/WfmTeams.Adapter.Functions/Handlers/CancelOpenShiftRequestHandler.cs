// ---------------------------------------------------------------------------
// <copyright file="CancelOpenShiftRequestHandler.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Services;

    public class CancelOpenShiftRequestHandler : OpenShiftRequestHandler
    {
        public CancelOpenShiftRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (CanHandleChangeRequest(changeRequest, OpenShiftRequestUriTemplate, out ChangeItemRequest itemRequest))
            {
                if (itemRequest.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
                {
                    changeItemRequest = itemRequest;
                }
                else if (itemRequest.Body != null)
                {
                    var openShiftRequest = itemRequest.Body.ToObject<OpenShiftsChangeRequest>();

                    if (openShiftRequest.AssignedTo.Equals(ChangeRequestAssignedTo.System, StringComparison.OrdinalIgnoreCase)
                        && openShiftRequest.State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase))
                    {
                        changeItemRequest = itemRequest;
                    }
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

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmOpenShiftRequest = openShiftRequest.AsWfmOpenShiftRequest();
            wfmOpenShiftRequest.BuId = connectionModel.WfmBuId;

            var wfmResponse = await _wfmActionService.CancelOpenShiftRequestAsync(wfmOpenShiftRequest, log).ConfigureAwait(false);

            if (wfmResponse.Success)
            {
                await _requestCacheService.DeleteRequestAsync(teamId, changeItemRequest.Id).ConfigureAwait(false);
                return new ChangeSuccessResult(changeResponse);
            }

            return WfmErrorToActionResult(wfmResponse.Error, changeItemRequest, changeResponse);
        }

        protected override Task<bool> MapOpenShiftRequestIdentitiesAsync(OpenShiftsChangeRequest openShiftRequest)
        {
            // this method is never called so nothing to do
            return Task.FromResult(true);
        }
    }
}
