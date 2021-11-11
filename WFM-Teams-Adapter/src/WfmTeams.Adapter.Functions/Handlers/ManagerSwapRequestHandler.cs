// ---------------------------------------------------------------------------
// <copyright file="ManagerSwapRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    using WfmTeams.Adapter.MicrosoftGraph.Enums;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ManagerSwapRequestHandler : SwapRequestHandler
    {
        public ManagerSwapRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
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
                    // this is an approval, but is it a manager approval
                    var swapRequest = itemRequest.Body.ToObject<SwapRequest>();
                    var state = swapRequest.EvaluateState(itemRequest.Method);
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
            Stopwatch sw = Stopwatch.StartNew();
            log.LogTrace($"{nameof(ManagerSwapRequestHandler)}:{nameof(HandleRequest)}:Started ({sw.ElapsedMilliseconds})");

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);

            var swapRequest = await ReadRequestObjectAsync<SwapRequest>(changeItemRequest, teamId).ConfigureAwait(false);
            if (swapRequest == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.SwapRequestNotFound, _stringLocalizer[ErrorCodes.SwapRequestNotFound]);
            }

            // are we already processing a request, if so block premature retries
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            if (changeData.ManagerStatus == ChangeData.RequestStatus.InProgress)
            {
                changeData = await WaitAndReadChangeDataAsync(swapRequest).ConfigureAwait(false);
                if (changeData.ManagerStatus == ChangeData.RequestStatus.InProgress)
                {
                    return new ChangeErrorResult(changeResponse, ErrorCodes.RequestInProgress, _stringLocalizer[ErrorCodes.RequestInProgress], HttpStatusCode.Processing);
                }
            }

            if (changeData.ManagerStatus == ChangeData.RequestStatus.Complete)
            {
                if (changeData.ManagerResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    // we have already processed and actioned this request and it was successful so
                    // don't try to perform the same action again because it will fail, however,
                    // because the first request succeeded (for us) we updated the cached shifts for
                    // the new ID's that Teams sent for the swapped shifts, however, because Teams
                    // is retrying, the shifts have new id's again, so we need to update the cache again
                    await ReconcileSwappedShiftsAsync(swapRequest, changeRequest, changeResponse, teamId, connectionModel.TimeZoneInfoId, true).ConfigureAwait(false);

                    return new ChangeSuccessResult(changeResponse);
                }

                return new ChangeErrorResult(changeResponse, changeItemRequest, changeData.ManagerResult.ErrorCode, changeData.ManagerResult.ErrorMessage);
            }

            await MapSwapRequestIdentitiesAsync(swapRequest, teamId).ConfigureAwait(false);
            if (string.IsNullOrEmpty(swapRequest.TargetManagerUserId) || string.IsNullOrEmpty(swapRequest.TargetManagerLoginName))
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            var state = swapRequest.EvaluateState(changeItemRequest.Method);
            var approved = state == ChangeRequestState.ManagerApproved;

            WfmShiftSwapModel wfmSwapRequest;
            WfmResponse wfmResponse;
            var cachedRequest = await _requestCacheService.LoadRequestAsync<SwapRequest>(teamId, changeItemRequest.Id).ConfigureAwait(false);
            if (cachedRequest != null)
            {
                var prevState = cachedRequest.EvaluateState(changeItemRequest.Method);
                if ((state == ChangeRequestState.ManagerApproved || state == ChangeRequestState.ManagerDeclined) && prevState == ChangeRequestState.RecipientPending)
                {
                    log.LogTrace($"{nameof(ManagerSwapRequestHandler)}:{nameof(HandleRequest)}:DoRecipientApproval ({sw.ElapsedMilliseconds})");

                    // this is a manager approval step, so as the previous state of the swap request
                    // (RecipientPending) indicates that a recipient approval was pending, we need
                    // to do the recipient approval step first and then do the manager approval
                    wfmSwapRequest = cachedRequest.AsWfmSwapRequest();
                    wfmSwapRequest.BuId = connectionModel.WfmBuId;
                    wfmResponse = await _wfmActionService.RecipientApproveShiftSwapRequestAsync(wfmSwapRequest, approved, log).ConfigureAwait(false);
                    var actionResult = WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);

                    if (actionResult is ChangeErrorResult || state == ChangeRequestState.ManagerDeclined)
                    {
                        // either the recipient approval failed or the original request was a
                        // decline in which case we can assume that the recipient decline is
                        // sufficient to end the swap request in the WFM provider - IS THIS TRUE OF
                        // ALL PROVIDERS?
                        return actionResult;
                    }
                }
            }

            log.LogTrace($"{nameof(ManagerSwapRequestHandler)}:{nameof(HandleRequest)}:DoManagerApproval ({sw.ElapsedMilliseconds})");

            changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            changeData.ManagerStatus = ChangeData.RequestStatus.InProgress;
            // clear other statuses to avoid blocking subsequent requests for the same two shifts
            ClearOtherStatuses(changeData);
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            wfmSwapRequest = swapRequest.AsWfmSwapRequest();
            wfmSwapRequest.BuId = connectionModel.WfmBuId;
            wfmResponse = await _wfmActionService.ManagerApproveShiftSwapRequestAsync(wfmSwapRequest, approved, log).ConfigureAwait(false);
            var approvalResult = WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);

            await _requestCacheService.SaveRequestAsync(teamId, changeItemRequest.Id, swapRequest).ConfigureAwait(false);

            if (approvalResult is ChangeSuccessResult && state == ChangeRequestState.ManagerApproved)
            {
                log.LogTrace($"{nameof(ManagerSwapRequestHandler)}:{nameof(HandleRequest)}:ReconcileSwappedShifts ({sw.ElapsedMilliseconds})");

                // we now need to update the cached shifts for the change to the ID's
                approvalResult = await ReconcileSwappedShiftsAsync(swapRequest, changeRequest, changeResponse, teamId, connectionModel.TimeZoneInfoId).ConfigureAwait(false);
            }

            // flag that the manager approval has now finished
            changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            changeData.ManagerStatus = ChangeData.RequestStatus.Complete;
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            log.LogTrace($"{nameof(ManagerSwapRequestHandler)}:{nameof(HandleRequest)}:Finished ({sw.ElapsedMilliseconds})");

            return approvalResult;
        }

        protected override async Task SaveChangeResultAsync(SwapRequest swapRequest, HttpStatusCode statusCode = HttpStatusCode.OK, string errorCode = "", string errorMessage = "")
        {
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
            // only update the result if there isn't an existing one
            if (changeData.ManagerResult == null)
            {
                changeData.ManagerResult = new ChangeResultModel
                {
                    StatusCode = (int)statusCode,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                };
                await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);
            }
        }

        private void ClearOtherStatuses(ChangeData changeData)
        {
            changeData.SenderStatus = ChangeData.RequestStatus.NotStarted;
            changeData.SenderResult = null;
            changeData.RecipientStatus = ChangeData.RequestStatus.NotStarted;
            changeData.RecipientResult = null;
        }

        private DateTime ConvertToLocalTime(DateTime dateTime, string timeZoneInfoId)
        {
            var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneInfoId);
            var localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeFromUtc(localDateTime, timezoneInfo);
        }

        private async Task MapSwapRequestIdentitiesAsync(SwapRequest swapRequest, string teamId)
        {
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapRequest.ManagerUserId).ConfigureAwait(false);
            if (employee == null && swapRequest.ManagerUserId.Equals(_teamOptions.GraphApiUserId, StringComparison.OrdinalIgnoreCase))
            {
                // the manager user is the acts as user account and may just be a service account
                // rather than an actual BY user in which case we should just select a random manager
                var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);

                var managerList = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, connectionModel.WfmBuId).ConfigureAwait(false);
                if (managerList != null && managerList.Count > 0)
                {
                    // get the first manager
                    employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, managerList[0]).ConfigureAwait(false);
                }
            }

            if (employee != null)
            {
                swapRequest.TargetManagerUserId = employee.WfmEmployeeId;
                swapRequest.TargetManagerLoginName = employee.WfmLoginName;
            }
        }

        private async Task<IActionResult> ReconcileSwappedShiftsAsync(SwapRequest swapRequest, ChangeRequest changeRequest, ChangeResponse changeResponse, string teamId, string timeZoneInfoId, bool subsequentAttempt = false)
        {
            var changeData = await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);

            // get the 2 deleted shift ids
            List<string> deletedShiftIds;
            if (subsequentAttempt)
            {
                // in the first attempt we updated the cached shifts with the new ID's that were
                // supplied in that attempt, however, this time we have comletely new shift ID's
                // therefore the first attempt new ID's are now the second or subsequent attempt
                // deleted ID's
                deletedShiftIds = changeData.ShiftIds;
            }
            else
            {
                deletedShiftIds = changeRequest.Requests
                    .Where(r => r.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Id)
                    .ToList();
            }

            // get the 2 new shifts
            var newShifts = changeRequest.Requests
                .Where(r => r.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Body.ToObject<ShiftResponse>())
                .ToList();

            // cache the ID's of the new shifts for a possible second attempt
            changeData.ShiftIds = newShifts.Select(s => s.Id).ToList();
            await SaveChangeDataAsync(swapRequest, changeData).ConfigureAwait(false);

            // load the up to 2 weeks of shift caches (if the shift swap should span a week
            // boundary) N.B. teams supplies the StartDateTime in UTC which means that when we try
            // and select the weekStartDate if the shift starts 00:15 local time - utc is 23:15 in
            // the previous day and hence previous week, consequently we need to use the local time
            // when selecting the weekStartDates
            var weekStartDates = newShifts
                .Select(s => ConvertToLocalTime(s.SharedShift.StartDateTime.Value, timeZoneInfoId).StartOfWeek(_teamOptions.StartDayOfWeek))
                .Distinct();

            var policy = GetConflictRetryPolicy(_teamOptions.RetryMaxAttempts, _teamOptions.RetryIntervalSeconds);
            foreach (var weekStartDate in weekStartDates)
            {
                await policy.ExecuteAsync(() => UpdateCachedShiftsAsync(teamId, weekStartDate, deletedShiftIds, newShifts, subsequentAttempt)).ConfigureAwait(false);
            }

            // return success for everything
            return new ChangeSuccessResult(changeResponse);
        }

        private async Task UpdateCachedShiftsAsync(string teamId, DateTime weekStartDate, List<string> deletedShiftIds, List<ShiftResponse> newShifts, bool subsequentAttempt)
        {
            // get the week cache with a lease to ensure that no other process can update it (min
            // 15s, max 1m)
            var cacheModel = await _scheduleCacheService.LoadScheduleWithLeaseAsync(teamId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);

            var cachedShifts = cacheModel.Tracked
                .Where(s => deletedShiftIds.Contains(s.TeamsShiftId))
                .ToList();

            if (cachedShifts.Count < 2)
            {
                // we didn't find both the deleted shifts in this week's cache, so just exit
                return;
            }

            if (!subsequentAttempt)
            {
                // update the cached shift with the new shift id
                foreach (var cachedShift in cachedShifts)
                {
                    var newShift = newShifts.Find(s => s.UserId != cachedShift.TeamsEmployeeId);
                    cachedShift.TeamsShiftId = newShift.Id;
                }

                // swap the Teams employee ID's to match what will have been done in Teams
                var teamsEmployeeId = cachedShifts[0].TeamsEmployeeId;
                cachedShifts[0].TeamsEmployeeId = cachedShifts[1].TeamsEmployeeId;
                cachedShifts[1].TeamsEmployeeId = teamsEmployeeId;

                // swap the WFM employee ID's to match what will have been done in WFM
                var employeeId = cachedShifts[0].WfmEmployeeId;
                cachedShifts[0].WfmEmployeeId = cachedShifts[1].WfmEmployeeId;
                cachedShifts[1].WfmEmployeeId = employeeId;
            }
            else
            {
                // just update the cached shift with the new shift id N.B. the users were swapped in
                // the first attempt hence the == on userid
                foreach (var cachedShift in cachedShifts)
                {
                    var newShift = newShifts.Find(s => s.UserId == cachedShift.TeamsEmployeeId);
                    cachedShift.TeamsShiftId = newShift.Id;
                }
            }

            await _scheduleCacheService.SaveScheduleWithLeaseAsync(teamId, weekStartDate, cacheModel).ConfigureAwait(false);
        }
    }
}
