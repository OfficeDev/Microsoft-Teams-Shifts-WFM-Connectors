// ---------------------------------------------------------------------------
// <copyright file="ManagerOpenShiftRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Enums;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ManagerOpenShiftRequestHandler : OpenShiftRequestHandler
    {
        public ManagerOpenShiftRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(teamOptions, featureOptions, scheduleConnectorService, scheduleCacheService, requestCacheService, secretsService, stringLocalizer, cacheService, wfmActionService)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (CanHandleChangeRequest(changeRequest, OpenShiftRequestUriTemplate, out ChangeItemRequest itemRequest))
            {
                if (itemRequest.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    // this is an approval/decline, but is it a manager approval/decline
                    var openShiftRequest = itemRequest.Body.ToObject<OpenShiftsChangeRequest>();
                    var state = openShiftRequest.EvaluateState(itemRequest.Method);
                    if (state == ChangeRequestState.ManagerApproved || state == ChangeRequestState.ManagerDeclined)
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

            if (!await MapOpenShiftRequestIdentitiesAsync(openShiftRequest).ConfigureAwait(false))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            var openShift = await GetOpenShift(teamId, openShiftRequest.OpenShiftId).ConfigureAwait(false);
            if (openShift == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.NoOpenShiftsFound, _stringLocalizer[ErrorCodes.NoOpenShiftsFound]);
            }

            var state = openShiftRequest.EvaluateState(changeItemRequest.Method);
            var approve = state == ChangeRequestState.ManagerApproved;

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            var wfmOpenShiftRequest = openShiftRequest.AsWfmOpenShiftRequest();
            wfmOpenShiftRequest.BuId = connectionModel.WfmBuId;
            wfmOpenShiftRequest.TimeZoneInfoId = connectionModel.TimeZoneInfoId;
            wfmOpenShiftRequest.WfmOpenShift = openShift;

            var wfmResponse = await _wfmActionService.ManagerApproveOpenShiftRequestAsync(wfmOpenShiftRequest, approve, log).ConfigureAwait(false);

            if (wfmResponse.Success && !string.IsNullOrEmpty(wfmResponse.NewEntityId))
            {
                await ManagerApproveAsync(openShift, wfmResponse.NewEntityId, openShiftRequest, changeRequest, teamId, connectionModel.TimeZoneInfoId, log).ConfigureAwait(false);
                return new ChangeSuccessResult(changeResponse);
            }

            // Shift was not claimed so we decline the request and return success
            await ManagerDeclineAsync(openShiftRequest, teamId).ConfigureAwait(false);
            return new ChangeSuccessResult(changeResponse);
        }

        protected override async Task<bool> MapOpenShiftRequestIdentitiesAsync(OpenShiftsChangeRequest openShiftRequest)
        {
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, openShiftRequest.ManagerUserId).ConfigureAwait(false);
            if (employee != null)
            {
                openShiftRequest.WfmManagerId = employee.WfmEmployeeId;
                openShiftRequest.WfmManagerLoginName = GetTargetLoginName(employee.WfmLoginName);
            }

            return true;
        }

        private async Task<bool> ManagerApproveAsync(ShiftModel openShift, string newWfmShiftId, OpenShiftsChangeRequest openShiftRequest, ChangeRequest changeRequest, string teamId, string timeZoneInfoId, ILogger log)
        {
            try
            {
                var weekStartDate = openShift.StartDate.ApplyTimeZoneOffset(timeZoneInfoId).StartOfWeek(_teamOptions.StartDayOfWeek);
                var policy = GetConflictRetryPolicy(_teamOptions.RetryMaxAttempts, _teamOptions.RetryIntervalSeconds);

                // as the manager has approved the request and it has been assigned successfully,
                // decrement the quantity
                openShift.Quantity--;

                // update the cache for the assignment
                await policy.ExecuteAsync(() => UpdateCachedOpenShiftsAsync(teamId, openShift, weekStartDate)).ConfigureAwait(false);

                // get the new shift created from the open shift in Teams
                var newShift = changeRequest.Requests
                    .Where(r => r.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Body.ToObject<ShiftResponse>())
                    .First();

                // convert the open shift to a shift
                openShift.Quantity = 1;
                openShift.WfmEmployeeId = openShiftRequest.WfmSenderId;
                openShift.TeamsEmployeeId = openShiftRequest.SenderUserId;
                openShift.TeamsShiftId = newShift.Id;
                openShift.WfmShiftId = newWfmShiftId;

                // and add it to the week shifts cache
                await policy.ExecuteAsync(() => UpdateCachedShiftsAsync(teamId, openShift, weekStartDate)).ConfigureAwait(false);

                // finally, remove the request from cache as we have finished with it
                await _requestCacheService.DeleteRequestAsync(teamId, openShiftRequest.Id).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                // the employee cannot claim this shift, so log it and try the next
                log.LogOpenShiftAssignmentError(ex, openShiftRequest.WfmSenderId, openShift.WfmShiftId, teamId);
            }

            return false;
        }

        private async Task<bool> ManagerDeclineAsync(OpenShiftsChangeRequest openShiftRequest, string teamId)
        {
            // when a manager declines an open shift request, there is nothing we need do other than
            // delete the request from the request cache
            await _requestCacheService.DeleteRequestAsync(teamId, openShiftRequest.Id).ConfigureAwait(false);
            return true;
        }

        private async Task UpdateCachedOpenShiftsAsync(string teamId, ShiftModel assignedOpenShift, DateTime weekStartDate)
        {
            var openShiftsScheduleId = GetSaveScheduleId(teamId);
            var schedule = await _scheduleCacheService.LoadScheduleWithLeaseAsync(openShiftsScheduleId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);
            var shift = schedule.Tracked.FirstOrDefault(s => s.WfmShiftId == assignedOpenShift.WfmShiftId);

            // update the quantity
            shift.Quantity = assignedOpenShift.Quantity;

            if (shift.Quantity <= 0)
            {
                schedule.Tracked.Remove(shift);
            }

            await _scheduleCacheService.SaveScheduleWithLeaseAsync(openShiftsScheduleId, weekStartDate, schedule).ConfigureAwait(false);
        }

        private async Task UpdateCachedShiftsAsync(string teamId, ShiftModel assignedShift, DateTime weekStartDate)
        {
            var schedule = await _scheduleCacheService.LoadScheduleWithLeaseAsync(teamId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);
            schedule.Tracked.Add(assignedShift);
            await _scheduleCacheService.SaveScheduleWithLeaseAsync(teamId, weekStartDate, schedule).ConfigureAwait(false);
        }
    }
}
