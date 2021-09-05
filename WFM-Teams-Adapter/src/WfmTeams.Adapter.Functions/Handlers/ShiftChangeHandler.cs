// ---------------------------------------------------------------------------
// <copyright file="ShiftChangeHandler.cs" company="Microsoft">
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
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Helpers;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ShiftChangeHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate ShiftChangeUriTemplate = new UriTemplate("/shifts/{id}");

        private readonly IScheduleCacheService _scheduleCacheService;
        private readonly ISystemTimeService _systemTimeService;
        private readonly TeamOrchestratorOptions _teamOptions;

        public ShiftChangeHandler(TeamOrchestratorOptions teamOptions, IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, IScheduleCacheService scheduleCacheService, ISystemTimeService systemTimeService)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _teamOptions = teamOptions ?? throw new ArgumentNullException(nameof(teamOptions));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _systemTimeService = systemTimeService ?? throw new ArgumentNullException(nameof(systemTimeService));
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            // at the time of creation, we only support the delete of shifts which are sent to the
            // integration as a put because when deleting a shift, Teams puts the shift into a draft
            // delete state which becomes final when the changes are shared
            return CanHandleChangeRequest(changeRequest, ShiftChangeUriTemplate, out changeItemRequest) && changeRequest.Requests.Length == 1 && changeItemRequest.Method.Equals("put", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            if (IsDeletedShift(changeItemRequest))
            {
                return await DeleteShiftAsync(changeItemRequest, changeResponse, teamId, log, starter).ConfigureAwait(false);
            }
            else
            {
                // the connector does not currently support this operation
                return new ChangeErrorResult(changeResponse, ErrorCodes.UnsupportedOperation, _stringLocalizer[ErrorCodes.UnsupportedOperation], HttpStatusCode.Forbidden);
            }
        }

        private async Task<IActionResult> DeleteShiftAsync(ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            var shift = await ScheduleCacheHelper.FindShiftByTeamsShiftIdAsync(changeItemRequest.Id, teamId, _teamOptions.PastWeeks, _teamOptions.FutureWeeks, _teamOptions.StartDayOfWeek, _scheduleCacheService, _systemTimeService).ConfigureAwait(false);
            if (shift != null)
            {
                var policy = GetConflictRetryPolicy(_teamOptions.RetryMaxAttempts, _teamOptions.RetryIntervalSeconds);
                try
                {
                    await policy.ExecuteAsync(() => RemoveShiftFromScheduleAsync(shift, teamId)).ConfigureAwait(false);
                    await ShareDelete(shift, teamId, starter).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.LogDeleteShiftException(ex, shift, teamId);
                    return new ChangeErrorResult(changeResponse, ErrorCodes.InternalError, _stringLocalizer[ErrorCodes.InternalError], HttpStatusCode.InternalServerError);
                }

                log.LogDeleteShiftSuccess(shift, teamId);
            }

            return new ChangeSuccessResult(changeResponse);
        }

        private bool IsDeletedShift(ChangeItemRequest changeItemRequest)
        {
            var shift = changeItemRequest.Body.ToObject<ShiftChangeRequest>();
            // when the user chooses delete for a shift, the SharedShift details are copied to the
            // DraftShift and the IsActive flag for the DraftShift is set to false. If the user then
            // shares the individual delete to commit it, the integration is called again and the
            // DraftShift is set to null and the IsActive flag of the SharedShift is set to false.
            // HOWEVER, if the user chooses to share the entire schedule then the drafts are
            // committed but the integration is not called again, therefore we must perform the work
            // of the delete in the draft stage but also be able to handle the secondary commit stage
            return shift?.DraftShift?.IsActive == false || shift?.SharedShift?.IsActive == false;
        }

        private async Task RemoveShiftFromScheduleAsync(ShiftModel shift, string teamId)
        {
            var weekStartDate = shift.StartDate.StartOfWeek(_teamOptions.StartDayOfWeek);
            var cacheModel = await _scheduleCacheService.LoadScheduleWithLeaseAsync(teamId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);
            cacheModel.Tracked.RemoveAll(s => s.TeamsShiftId.Equals(shift.TeamsShiftId, StringComparison.OrdinalIgnoreCase));
            await _scheduleCacheService.SaveScheduleWithLeaseAsync(teamId, weekStartDate, cacheModel).ConfigureAwait(false);
        }

        private async Task ShareDelete(ShiftModel shift, string teamId, IDurableOrchestrationClient starter)
        {
            var deferredActionModel = new DeferredActionModel
            {
                ActionType = DeferredActionModel.DeferredActionType.ShareTeamSchedule,
                DelaySeconds = _teamOptions.DelayedActionSeconds,
                ShareStartDate = shift.StartDate,
                ShareEndDate = shift.EndDate,
                TeamId = teamId
            };
            await starter.StartNewAsync(nameof(DeferredActionOrchestrator), deferredActionModel).ConfigureAwait(false);
        }
    }
}
