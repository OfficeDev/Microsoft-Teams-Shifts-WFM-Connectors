// ---------------------------------------------------------------------------
// <copyright file="ManagerAssignOpenShiftHandler.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    [System.Runtime.InteropServices.Guid("8C87DD8E-86ED-4AF7-A421-29F7190DA3F9")]
    public class ManagerAssignOpenShiftHandler : ChangeRequestHandler
    {
        private readonly ICacheService _cacheService;
        private readonly IScheduleCacheService _scheduleCacheService;
        private readonly TeamOrchestratorOptions _teamOptions;
        protected readonly IWfmActionService _wfmActionService;

        public ManagerAssignOpenShiftHandler(TeamOrchestratorOptions teamOptions, IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<Triggers.ChangeRequestTrigger> stringLocalizer, IScheduleCacheService scheduleCacheService, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _teamOptions = teamOptions ?? throw new ArgumentNullException(nameof(teamOptions));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmActionService = wfmActionService ?? throw new ArgumentNullException(nameof(wfmActionService));
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            // in order for this handler to be able to handle this open shift change, it must
            // contain exactly two requests, one for the open shift being assigned and one for the
            // shift that is to be created from it
            if (changeRequest.Requests.Length == 2
                && CanHandleChangeRequest(changeRequest, OpenShiftChangeHandler.OpenShiftChangeUriTemplate, out ChangeItemRequest itemRequest)
                && CanHandleChangeRequest(changeRequest, ShiftChangeHandler.ShiftChangeUriTemplate, out _))
            {
                changeItemRequest = itemRequest;
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            // get the open shift from the change item
            var openShift = changeItemRequest.Body.ToObject<OpenShiftResponse>();
            // get the proposed shift to be created from the open shift
            var proposedShift = changeRequest.Requests
                .Where(r => r.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Body.ToObject<ShiftResponse>())
                .First();

            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);

            // get the corresponding open shift from cache
            var localStartDate = openShift.SharedOpenShift.StartDateTime.ApplyTimeZoneOffset(connectionModel.TimeZoneInfoId);
            var weekStartDate = localStartDate.StartOfWeek(_teamOptions.StartDayOfWeek);
            var scheduleId = teamId + ApplicationConstants.OpenShiftsSuffix;
            var cacheModel = await _scheduleCacheService.LoadScheduleAsync(scheduleId, weekStartDate).ConfigureAwait(false);
            var assignedShift = cacheModel.Tracked.FirstOrDefault(o => o.TeamsShiftId == openShift.Id);
            if (assignedShift == null)
            {
                // we didn't find an open shift with this ID
                return new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.NoOpenShiftsFound, _stringLocalizer[ErrorCodes.NoOpenShiftsFound]);
            }

            // get the employee object for the manager who initiated the change
            var manager = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, openShift.LastModifiedBy.User.Id).ConfigureAwait(false);
            if (manager == null)
            {
                return new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            // get the employee object for the user the open shift is assigned to
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, proposedShift.UserId).ConfigureAwait(false);
            if (employee == null)
            {
                return new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            var wfmResponse = await _wfmActionService.ManagerAssignOpenShiftAsync(assignedShift, manager, employee, connectionModel.WfmBuId, connectionModel.TimeZoneInfoId, log).ConfigureAwait(false);
            if (!wfmResponse.Success)
            {
                return new ChangeErrorResult(changeResponse, changeItemRequest, wfmResponse.Error.Code, wfmResponse.Error.Message);
            }

            var policy = GetConflictRetryPolicy(_teamOptions.RetryMaxAttempts, _teamOptions.RetryIntervalSeconds);

            // as it has been assigned successfully, decrement the quantity
            assignedShift.Quantity--;

            // update the open shift in the cache
            await policy.ExecuteAsync(() => UpdateCachedOpenShiftsAsync(scheduleId, assignedShift, weekStartDate)).ConfigureAwait(false);

            // convert the open shift to a shift and add it to the week shifts cache
            assignedShift.Quantity = 1;
            assignedShift.WfmEmployeeId = employee.WfmEmployeeId;
            assignedShift.TeamsEmployeeId = employee.TeamsEmployeeId;
            assignedShift.TeamsShiftId = proposedShift.Id;
            assignedShift.WfmShiftId = wfmResponse.NewEntityId;

            await policy.ExecuteAsync(() => UpdateCachedShiftsAsync(teamId, assignedShift, weekStartDate)).ConfigureAwait(false);

            // finally, set up a deferred action to share the schedule
            var deferredActionModel = new DeferredActionModel
            {
                ActionType = DeferredActionModel.DeferredActionType.ShareTeamSchedule,
                DelaySeconds = _teamOptions.DelayedActionSeconds,
                ShareStartDate = openShift.SharedOpenShift.StartDateTime.Date,
                ShareEndDate = openShift.SharedOpenShift.EndDateTime.Date.AddHours(23).AddMinutes(59),
                TeamId = teamId
            };
            await starter.StartNewAsync(nameof(DeferredActionOrchestrator), deferredActionModel).ConfigureAwait(false);

            return new ChangeSuccessResult(changeResponse);
        }

        private async Task UpdateCachedOpenShiftsAsync(string scheduleId, ShiftModel assignedOpenShift, DateTime weekStartDate)
        {
            var schedule = await _scheduleCacheService.LoadScheduleWithLeaseAsync(scheduleId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);
            var shift = schedule.Tracked.FirstOrDefault(s => s.WfmShiftId == assignedOpenShift.WfmShiftId);

            // update the quantity
            shift.Quantity = assignedOpenShift.Quantity;

            if (shift.Quantity <= 0)
            {
                schedule.Tracked.Remove(shift);
            }

            await _scheduleCacheService.SaveScheduleWithLeaseAsync(scheduleId, weekStartDate, schedule).ConfigureAwait(false);
        }

        private async Task UpdateCachedShiftsAsync(string teamId, ShiftModel assignedShift, DateTime weekStartDate)
        {
            var schedule = await _scheduleCacheService.LoadScheduleWithLeaseAsync(teamId, weekStartDate, new TimeSpan(0, 0, _teamOptions.StorageLeaseTimeSeconds)).ConfigureAwait(false);
            schedule.Tracked.Add(assignedShift);
            await _scheduleCacheService.SaveScheduleWithLeaseAsync(teamId, weekStartDate, schedule).ConfigureAwait(false);
        }
    }
}
