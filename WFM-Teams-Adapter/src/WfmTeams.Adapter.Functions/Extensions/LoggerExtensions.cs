// ---------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Extensions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.MicrosoftGraph.Exceptions;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;

    public static class LoggerExtensions
    {
        public static void LogActivity(this ILogger log, TeamActivityModel activityModel)
        {
            log.LogInformation(EventIds.Activity, "Activity: Type={activityType}, TeamId={teamId}, DateValue={dateValue}, WfmBuId={wfmBuId}", activityModel.ActivityType, activityModel.TeamId, activityModel.DateValue, activityModel.WfmBuId);
        }

        public static void LogActivitySkipped(this ILogger log, TeamActivityModel activityModel, string reason)
        {
            log.LogInformation(EventIds.Activity, "Activity: Status={status}, Type={activityType}, TeamId={teamId}, DateValue={dateValue}, WfmBuId={wfmBuId}, Reason={reason}", Status.Skipped, activityModel.ActivityType, activityModel.TeamId, activityModel.DateValue, activityModel.WfmBuId, reason);
        }

        public static void LogAggregateOrchestrationError(this ILogger log, AggregateException ex, TeamModel team, string orchestratorName)
        {
            if (ex == null)
                return;

            foreach (var iex in ex.InnerExceptions)
            {
                if (iex is AggregateException aggregateException)
                {
                    // recursively log the aggregate exception
                    log.LogAggregateOrchestrationError(aggregateException, team, orchestratorName);
                }
                else
                {
                    log.LogOrchestrationError(iex, team, orchestratorName);
                }
            }
        }

        public static void LogAppliedDelta<T>(this ILogger log, string teamId, string dateValue, DeltaModel<T> delta, string itemType = "Shifts") where T : IDeltaItem
        {
            log.LogDelta(Stage.Applied, teamId, dateValue, delta, itemType);
        }

        public static void LogApproveSwapShiftsRequestActivity(this ILogger log, DeferredActionModel delayedActionModel)
        {
            log.LogInformation(EventIds.ApproveSwapShiftsRequest, "ApproveSwapShiftsRequest: Stage={stage} RequestId={requestId}, TeamId={teamId}, Message={message}", Stage.Start, delayedActionModel.RequestId, delayedActionModel.TeamId, delayedActionModel.Message);
        }

        public static void LogApproveSwapShiftsRequestActivityFailure(this ILogger log, Exception ex, DeferredActionModel delayedActionModel)
        {
            log.LogError(EventIds.ApproveSwapShiftsRequest, ex, "ApproveSwapShiftsRequest: Status={status} RequestId={requestId}, TeamId={teamId}, Message={message}", Status.Failed, delayedActionModel.RequestId, delayedActionModel.TeamId, delayedActionModel.Message);
        }

        public static void LogAvailabilityError(this ILogger log, MicrosoftGraphException ex, string teamId, string operationName, EmployeeAvailabilityModel availability)
        {
            log.LogError(EventIds.Availability, ex, "Availability: Status={status}, OperationName={operationName}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, SourceId={sourceId}, EmployeeId={employeeId}, StartDate={startDate}, EndDate={endDate}, TeamId={teamId}", Status.Failed, operationName, ex.Error.Code, ex.Error.Message, ex.Error.InnerError?.RequestId, ex.Error.InnerError?.Date, availability.WfmId, availability.WfmEmployeeId, availability.StartDate.AsDateString(), availability.EndDate.AsDateString(), teamId);
        }

        public static void LogAvailabilityError(this ILogger log, Exception ex, string teamId, string operationName, EmployeeAvailabilityModel availability)
        {
            log.LogError(EventIds.Availability, ex, "Availability: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, StartDate={startDate}, EndDate={endDate}, TeamId={teamId}", Status.Failed, operationName, availability.WfmId, availability.WfmEmployeeId, availability.StartDate.AsDateString(), availability.EndDate.AsDateString(), teamId);
        }

        public static void LogAvailabilitySkipped(this ILogger log, string teamId, string operationName, EmployeeAvailabilityModel availability)
        {
            log.LogTrace(EventIds.Availability, "Availability: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, StartDate={startDate}, EndDate={endDate}, TeamId={teamId}", Status.Skipped, operationName, availability.WfmId, availability.WfmEmployeeId, availability.StartDate.AsDateString(), availability.EndDate.AsDateString(), teamId);
        }

        public static void LogChangeError(this ILogger log, Exception ex, string entityId)
        {
            log.LogError(EventIds.Change, ex, "Change: Stage={stage}, EntityId={entityId}", Stage.Request, entityId);
        }

        public static void LogChangeRequest(this ILogger log, ChangeRequest request, string entityId)
        {
            var changeBody = JsonConvert.SerializeObject(request, Formatting.None);
            changeBody = changeBody.Replace("{", "(").Replace("}", ")");
            log.LogTrace(EventIds.Change, "Change: Stage={stage}, Body={changeBody}, EntityId={entityId}", Stage.Request, changeBody, entityId);

            foreach (var changeItemRequest in request.Requests)
            {
                log.LogInformation(EventIds.Change, "Change: Stage={stage}, Id={changeItemRequestId}, Method={changeItemRequestMethod}, Url={changeItemRequestUrl}, EntityId={entityId}", Stage.Request, changeItemRequest.Id, changeItemRequest.Method, changeItemRequest.Url, entityId);
            }
        }

        public static void LogChangeResult(this ILogger log, IActionResult result)
        {
            if (result is ChangeSuccessResult)
            {
                var changeResponse = (ChangeResponse)((ChangeSuccessResult)result).Value;
                foreach (var itemResponse in changeResponse.Responses)
                {
                    log.LogInformation(EventIds.Change, "Change: Stage={stage}, ItemId={itemId}, ItemStatus={itemStatus}, ItemEtag={itemEtag}", Stage.End, itemResponse.Id, itemResponse.Status, itemResponse.Body?.Etag);
                }
            }
            else if (result is ChangeErrorResult)
            {
                var changeResponse = (ChangeResponse)((ChangeErrorResult)result).Value;
                foreach (var itemResponse in changeResponse.Responses)
                {
                    log.LogError(EventIds.Change, "Change: Stage={stage}, ItemId={itemId}, ItemStatus={itemStatus}, ItemErrorCode={itemErrorCode}, ItemErrorMessage={itemErrorMessage}, ItemEtag={itemEtag}", Stage.End, itemResponse.Id, itemResponse.Status, itemResponse.Body?.Error?.Code, itemResponse.Body?.Error?.Message, itemResponse.Body?.Etag);
                }
            }
        }

        public static void LogClearEnd(this ILogger log, ClearScheduleModel clear, string clearType, ResultModel resultModel)
        {
            log.Log(resultModel.LogLevel, EventIds.ClearSchedule, "Clear{clearType}: Stage={stage}, TeamId={teamId}, StartDate={startDate}, EndDate={endDate}, UtcStartDate={utcStartDate}, UtcEndDate={utcEndDate}, DeletedCount={deletedCount}", clearType, Stage.End, clear.TeamId, clear.StartDate.AsDateString(), clear.EndDate.AsDateString(), clear.UtcStartDate.AsDateString(), clear.UtcEndDate.AsDateString(), resultModel.DeletedCount);
        }

        public static void LogClearStart(this ILogger log, ClearScheduleModel clear, string clearType)
        {
            log.LogInformation(EventIds.ClearSchedule, "Clear{clearType}: Stage={stage}, TeamId={teamId}, StartDate={startDate}, EndDate={endDate}, UtcStartDate={utcStartDate}, UtcEndDate={utcEndDate}, ClearOpenShifts={clearOpenShifts}, ClearSchedulingGroups={clearSchedulingGroups}, ClearShifts={clearShifts}, ClearTimeOff={clearTimeOff}", clearType, Stage.Start, clear.TeamId, clear.StartDate.AsDateString(), clear.EndDate.AsDateString(), clear.UtcStartDate.AsDateString(), clear.UtcEndDate.AsDateString(), clear.ClearOpenShifts, clear.ClearSchedulingGroups, clear.ClearShifts, clear.ClearTimeOff);
        }

        public static void LogDeleteShiftException(this ILogger log, Exception ex, ShiftModel shift, string teamId)
        {
            log.LogError(EventIds.Shift, ex, "DeleteShift: Status={status}, TeamId={teamId}, WfmShiftId={wfmShiftId}, WfmEmployeeId={wfmEmployeeId}, TeamsShiftId={teamsShiftId}, TeamsEmployeeId={teamsEmployeeId}", Status.Failed, teamId, shift.WfmShiftId, shift.WfmEmployeeId, shift.TeamsShiftId, shift.TeamsEmployeeId);
        }

        public static void LogDeleteShiftSuccess(this ILogger log, ShiftModel shift, string teamId)
        {
            log.LogInformation(EventIds.Shift, "DeleteShift: Status={status}, TeamId={teamId}, WfmShiftId={wfmShiftId}, WfmEmployeeId={wfmEmployeeId}, TeamsShiftId={teamsShiftId}, TeamsEmployeeId={teamsEmployeeId}", Status.Success, teamId, shift.WfmShiftId, shift.WfmEmployeeId, shift.TeamsShiftId, shift.TeamsEmployeeId);
        }

        public static void LogDelta<T>(this ILogger log, string stage, string teamId, string dateValue, DeltaModel<T> delta, string itemType = "Shifts") where T : IDeltaItem
        {
            log.LogInformation(EventIds.Delta, "Delta_{itemType}: Stage={stage}, Created={createdCount}, Updated={updatedCount}, Deleted={deletedCount}, Failed={failedCount}, Skipped={skippedCount}, TeamId={teamId}, DateValue={dateValue}", itemType, stage, delta.Created.Count, delta.Updated.Count, delta.Deleted.Count, delta.Failed.Count, delta.Skipped.Count, teamId, dateValue);
        }

        public static void LogDepartmentNotFound(this ILogger log, TeamActivityModel activityModel, ShiftModel shift)
        {
            log.LogWarning(EventIds.Department, "Department: Status={status}, JobId={jobId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", Status.NotFound, shift.WfmJobId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogDisableOrchestrators(this ILogger log, string teamId)
        {
            log.LogInformation(EventIds.Orchestrator, "Orchestrators: Status={status}, TeamId={teamId}", Status.Disabled, teamId);
        }

        public static void LogEmployeeCacheActivityInfo(this ILogger log, string teamId, int teamsCount, int sourceCount, int sourceManagersCount)
        {
            log.LogInformation(EventIds.EmployeeCacheRefresh, "EmployeeCacheRefresh: TeamId={teamId}, TeamsCount={teamsCount}, SourceCount={sourceCount}, SourceManagersCount={sourceManagersCount}", teamId, teamsCount, sourceCount, sourceManagersCount);
        }

        public static void LogEnableOrchestrators(this ILogger log, string teamId)
        {
            log.LogInformation(EventIds.Orchestrator, "Orchestrators: Status={status}, TeamId={teamId}", Status.Enabled, teamId);
        }

        public static void LogFullDelta<T>(this ILogger log, string teamId, string dateValue, DeltaModel<T> delta, string itemType = "Shifts") where T : IDeltaItem
        {
            log.LogDelta(Stage.Full, teamId, dateValue, delta, itemType);
        }

        public static void LogJobNotFound(this ILogger log, TeamActivityModel activityModel, ActivityModel job)
        {
            log.LogWarning(EventIds.Job, "Job: Status={status}, JobId={jobId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", Status.NotFound, job.WfmJobId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogMetrics(this ILogger log, IDictionary<string, object> properties, ResultModel result, string metricPrefix = "Shifts")
        {
            log.LogMetric(metricPrefix + nameof(result.CreatedCount), result.CreatedCount, properties);
            log.LogMetric(metricPrefix + nameof(result.UpdatedCount), result.UpdatedCount, properties);
            log.LogMetric(metricPrefix + nameof(result.DeletedCount), result.DeletedCount, properties);
            log.LogMetric(metricPrefix + nameof(result.FailedCount), result.FailedCount, properties);
            log.LogMetric(metricPrefix + nameof(result.SkippedCount), result.SkippedCount, properties);
        }

        public static void LogMissingTeam(this ILogger log, ConnectionModel connectionModel)
        {
            log.LogWarning(EventIds.MissingTeam, "MissingTeam: TeamId={teamId}, TeamName={teamName}, WfmBuId={wfmBuId}, WfmBuName={wfmBuName}", connectionModel.TeamId, connectionModel.TeamName, connectionModel.WfmBuId, connectionModel.WfmBuName);
        }

        public static void LogOpenShiftAssignmentError(this ILogger log, Exception ex, string employeeId, string openShiftId, string teamId)
        {
            log.LogError(EventIds.OpenShiftAssignment, ex, "OpenShiftAssignment: Status={status}, EmployeeId={employeeId}, OpenShiftId={openShiftId}, TeamId={teamId}", Status.Failed, employeeId, openShiftId, teamId);
        }

        public static void LogOrchestrationError(this ILogger log, Exception ex, TeamModel team, string orchestratorName)
        {
            log.LogError(EventIds.Orchestrator, ex, "Team: Status={status}, WfmBuId={wfmBuId}, TeamId={teamId}, Orchestrator={orchestratorName}", Status.Failed, team.WfmBuId, team.TeamId, orchestratorName);
        }

        public static void LogOrchestratorTimerTriggerConnectionsCount(this ILogger log, int totalCount, int enabledCount)
        {
            log.LogInformation(EventIds.Orchestrator, "OrchestratorTimerTrigger: ConnectionCounts: Total={totalCount}, Enabled={enabledCount}, Disabled={disabledCount}", totalCount, enabledCount, totalCount - enabledCount);
        }

        public static void LogOrchestratorTimerTriggerError(this ILogger log, Exception ex, string executionStage, long executionTime)
        {
            log.LogError(EventIds.Orchestrator, ex, "OrchestratorTimerTrigger: Status={status} ExecutionStage={executionStage}, ExecutionTimeMs={executionTime}", Status.Failed, executionStage, executionTime);
        }

        public static void LogOrchestratorTimerTriggerSuccess(this ILogger log, long executionTime)
        {
            log.LogInformation(EventIds.Orchestrator, "OrchestratorTimerTrigger: Status={status}, ExecutionTimeMs={executionTime}", Status.Success, executionTime);
        }

        public static void LogOrchestratorTimerTriggerSuspended(this ILogger log)
        {
            log.LogInformation(EventIds.Orchestrator, "OrchestratorTimerTrigger: Status={status}", Status.Suspended);
        }

        public static void LogPartialDelta<T>(this ILogger log, string teamId, string dateValue, DeltaModel<T> delta, string itemType = "Shifts") where T : IDeltaItem
        {
            log.LogDelta(Stage.Partial, teamId, dateValue, delta, itemType);
        }

        public static void LogPendingOrchestrator(this ILogger log, string orchestratorName, DateTime lastUpdatedTime, string teamId)
        {
            log.LogWarning(EventIds.Orchestrator, "Orchestrator: Status={status}, Name={orchestratorName}, LastUpdatedTime={lastUpdatedTime}, TeamId={teamId}", Status.Pending, orchestratorName, lastUpdatedTime.AsDateTimeString(), teamId);
        }

        public static void LogPendingOrchestratorsDetail(this ILogger log, string orchestratorName, int pendingCount, int skip, int take)
        {
            log.LogInformation(EventIds.Orchestrator, "Orchestrator: Name={orchestratorName}, PendingCount={pendingCount}, Skip={skip}, Take={take}", orchestratorName, pendingCount, skip, take);
        }

        public static void LogReviewOpenShiftRequestActivity(this ILogger log, DeferredActionModel delayedActionModel)
        {
            log.LogInformation(EventIds.ReviewOpenShiftRequest, "ReviewOpenShiftRequest: Stage={stage} RequestId={requestId}, TeamId={teamId}, Message={message}", Stage.Start, delayedActionModel.RequestId, delayedActionModel.TeamId, delayedActionModel.Message);
        }

        public static void LogReviewOpenShiftRequestActivityFailure(this ILogger log, Exception ex, DeferredActionModel delayedActionModel)
        {
            log.LogError(EventIds.ReviewOpenShiftRequest, ex, "ReviewOpenShiftRequest: Status={status} RequestId={requestId}, TeamId={teamId}, Message={message}", Status.Failed, delayedActionModel.RequestId, delayedActionModel.TeamId, delayedActionModel.Message);
        }

        public static void LogSchedule(this ILogger log, TeamModel team, ScheduleModel schedule)
        {
            var workforceIntegrationId = String.Empty;
            if (schedule.WorkforceIntegrationIds.Count > 0)
            {
                workforceIntegrationId = schedule.WorkforceIntegrationIds[0];
            }

            log.LogInformation(EventIds.Schedule, "Schedule: Status={status}, IsEnabled={isEnabled}, TimeZone={timeZone}, TeamId={teamId}, WorkforceIntegrationId={workforceIntegrationId}, OfferShiftRequestsEnabled={offerShiftRequestsEnabled}, OpenShiftsEnabled={openShiftsEnabled}, SwapShiftsRequestsEnabled={swapShiftsRequestsEnabled}, TimeClockEnabled={timeClockEnabled}, TimeOffRequestsEnabled={timeOffRequestsEnabled}", schedule.Status, schedule.IsEnabled, schedule.TimeZone, team.TeamId, workforceIntegrationId, schedule.OfferShiftRequestsEnabled, schedule.OpenShiftsEnabled, schedule.SwapShiftsRequestsEnabled, schedule.TimeClockEnabled, schedule.TimeOffRequestsEnabled);
        }

        public static void LogSchedulingGroupError(this ILogger log, Exception ex, TeamActivityModel activityModel, ShiftModel shift)
        {
            log.LogError(EventIds.SchedulingGroup, ex, "Scheduling Group: Status={status}, DepartmentName={departmentName}, SourceId={sourceId}, EmployeeId={employeeId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, shift.DepartmentName, shift.WfmShiftId, shift.WfmEmployeeId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogSchedulingGroupError(this ILogger log, MicrosoftGraphException ex, TeamActivityModel activityModel, ShiftModel shift)
        {
            log.LogError(EventIds.SchedulingGroup, ex, "Scheduling Group: Status={status}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, DepartmentName={departmentName}, SourceId={sourceId}, EmployeeId={employeeId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, ex.Error.Code, ex.Error.Message, ex.Error.InnerError?.RequestId, ex.Error.InnerError?.Date, shift.DepartmentName, shift.WfmShiftId, shift.WfmEmployeeId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogSchedulingGroupError(this ILogger log, Exception ex, TeamActivityModel activityModel, string departmentName, string schedulingGroupId)
        {
            log.LogError(EventIds.SchedulingGroup, ex, "Scheduling Group: Status={status}, DepartmentName={departmentName}, SchedulingGroupId={schedulingGroupId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, departmentName, schedulingGroupId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogShareSchedule(this ILogger log, DateTime startDate, DateTime endDate, bool notifyTeamOnChange, string teamId)
        {
            log.LogInformation(EventIds.ShareSchedule, "ShareSchedule: StartDate={startDate} EndDate={endDate}, NotifyTeamOnChange={notifyTeamOnChange}, TeamId={teamId}", startDate.AsDateTimeString(), endDate.AsDateTimeString(), notifyTeamOnChange, teamId);
        }

        public static void LogShiftError(this ILogger log, Exception ex, TeamActivityModel activityModel, string operationName, ShiftModel shift)
        {
            if (ex is MicrosoftGraphException mex)
            {
                log.LogError(EventIds.Shift, ex, "{shiftType}: Status={status}, OperationName={operationName}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, WfmShiftId={wfmShiftId}, WfmEmployeeId={wfmEmployeeId}, TeamsShiftId={teamsShiftId}, TeamsEmployeeId={teamsEmployeeId}, TeamsGroupId={teamsGroupId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", activityModel.ActivityType, Status.Failed, operationName, mex.Error.Code, mex.Error.Message, mex.Error.InnerError?.RequestId, mex.Error.InnerError?.Date, shift.WfmShiftId, shift.WfmEmployeeId, shift.TeamsShiftId ?? "Not Set", shift.TeamsEmployeeId ?? "Not Set", shift.TeamsSchedulingGroupId ?? "Not Set", activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
            }
            else
            {
                log.LogError(EventIds.Shift, ex, "{shiftType}: Status={status}, OperationName={operationName}, WfmShiftId={wmfShiftId}, WfmEmployeeId={wfmEmployeeId}, TeamsShiftId={teamsShiftId}, TeamsEmployeeId={teamsEmployeeId}, TeamsGroupId={teamsGroupId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", activityModel.ActivityType, Status.Failed, operationName, shift.WfmShiftId, shift.WfmEmployeeId, shift.TeamsShiftId ?? "Not Set", shift.TeamsEmployeeId ?? "Not Set", shift.TeamsSchedulingGroupId ?? "Not Set", activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
            }
        }

        public static void LogShiftError(this ILogger log, Exception ex, ClearScheduleModel clear, string operationName, ShiftModel shift, string shiftType = "Shift")
        {
            log.LogError(EventIds.ClearSchedule, ex, "{shiftType}: Status={status}, OperationName={operationName}, SourceId={sourceId}, DestinationId={destinationId}, EmployeeId={employeeId}, TeamId={teamId}, DayDate={dayDate}", shiftType, Status.Failed, operationName, shift.WfmShiftId, shift.TeamsShiftId, shift.WfmEmployeeId, clear.TeamId, clear.StartDate.AsDateString());
        }

        public static void LogShiftError(this ILogger log, Exception ex, ClearScheduleModel clear, string operationName, string shiftType = "Shift")
        {
            log.LogError(EventIds.ClearSchedule, ex, "{shiftType}: Status={status}, OperationName={operationName}, TeamId={teamId}, DayDate={dayDate}", shiftType, Status.Failed, operationName, clear.TeamId, clear.StartDate.AsDateString());
        }

        public static void LogShiftSkipped(this ILogger log, TeamActivityModel activityModel, string operationName, ShiftModel shift)
        {
            log.LogTrace(EventIds.Shift, "{shiftType}: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, WfmBuId={wfmBuId}, TeamId={teamId}, WeekDate={weekDate}", activityModel.ActivityType, Status.Skipped, operationName, shift.WfmShiftId, shift.WfmEmployeeId, activityModel.WfmBuId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogSourceRecords(this ILogger log, int recordCount, TeamActivityModel activityModel)
        {
            log.LogInformation(EventIds.Source, "Source: {activityType}={recordCount}, TeamId={teamId}, DateValue={dateValue}, WfmBuId={wfmBuId}", activityModel.ActivityType, recordCount, activityModel.TeamId, activityModel.DateValue, activityModel.WfmBuId);
        }

        public static void LogStalledOrchestrator(this ILogger log, string orchestratorName, DateTime lastUpdatedTime, DateTime hungThresholdTime, string teamId)
        {
            log.LogWarning(EventIds.Orchestrator, "Orchestrator: Status={status}, Name={orchestratorName}, LastUpdatedTime={lastUpdatedTime}, HungThresholdTime={hungThresholdTime}, TeamId={teamId}", Status.Stalled, orchestratorName, lastUpdatedTime.AsDateTimeString(), hungThresholdTime.AsDateTimeString(), teamId);
        }

        public static void LogStartOrchestrator(this ILogger log, string orchestratorName, string teamId)
        {
            log.LogInformation(EventIds.Orchestrator, "Orchestrator: Stage={stage}, Status={status}, Name={orchestratorName}, TeamId={teamId}", Stage.Start, Status.Valid, orchestratorName, teamId);
        }

        public static void LogStopOrchestrator(this ILogger log, string orchestratorName, string teamId)
        {
            log.LogInformation(EventIds.Orchestrator, "Orchestrator: Stage={stage}, Status={status}, Name={orchestratorName}, TeamId={teamId}", Stage.End, Status.Valid, orchestratorName, teamId);
        }

        public static void LogSubscribeTeam(this ILogger log, ConnectionModel connectionModel)
        {
            log.LogInformation(EventIds.Team, "Team: Status={status}, TeamId={teamId}, TeamName={teamName}, WfmBuId={wfmBuId}, WfmBuName={wfmBuName}", Status.Subscribed, connectionModel.TeamId, connectionModel.TeamName, connectionModel.WfmBuId, connectionModel.WfmBuName);
        }

        public static void LogTimeOffError(this ILogger log, MicrosoftGraphException ex, TeamActivityModel activityModel, string operationName, TimeOffModel timeOff)
        {
            log.LogError(EventIds.TimeOff, ex, "TimeOff: Status={status}, OperationName={operationName}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, SourceId={sourceId}, EmployeeId={employeeId}, TeamId={teamId}, Year={year}", Status.Failed, operationName, ex.Error.Code, ex.Error.Message, ex.Error.InnerError?.RequestId, ex.Error.InnerError?.Date, timeOff.WfmTimeOffId, timeOff.WfmEmployeeId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogTimeOffError(this ILogger log, Exception ex, TeamActivityModel activityModel, string operationName, TimeOffModel timeOff)
        {
            log.LogError(EventIds.TimeOff, ex, "TimeOff: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, TeamId={teamId}, Year={year}", Status.Failed, operationName, timeOff.WfmTimeOffId, timeOff.WfmEmployeeId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogTimeOffError(this ILogger log, Exception ex, ClearScheduleModel clearModel, string operationName, TimeOffModel timeOff)
        {
            log.LogError(EventIds.TimeOff, ex, "TimeOff: Status={status}, OperationName={operationName}, DestinationId={destinationId}, EmployeeId={employeeId}, TeamId={teamId}, StartDate={startDate}, EndDate={endDate}", Status.Failed, operationName, timeOff.TeamsTimeOffId, timeOff.TeamsEmployeeId, clearModel.TeamId, clearModel.StartDate.AsDateString(), clearModel.EndDate.AsDateString());
        }

        public static void LogTimeOffError(this ILogger log, Exception ex, ClearScheduleModel clear, string operationName)
        {
            log.LogError(EventIds.ClearSchedule, ex, "TimeOff: Status={status}, OperationName={operationName}, TeamId={teamId}, StartDate={startDate}, EndDate={endDate}, UtcStartDate={utcStartDate}, UtcEndDate={utcEndDate}", Status.Failed, operationName, clear.TeamId, clear.StartDate.AsDateString(), clear.EndDate.AsDateString(), clear.UtcStartDate.AsDateString(), clear.UtcEndDate.AsDateString());
        }

        public static void LogTimeOffError(this ILogger log, Exception ex, string operationName)
        {
            log.LogError(EventIds.TimeOff, ex, "TimeOff: Status={status}, OperationName={operationName}", Status.Failed, operationName);
        }

        public static void LogTimeOffOrchestratorStartError(this ILogger log, string runtimeStatus)
        {
            log.LogError(EventIds.TimeOff, "TimeOffOrchestrator: Status={status}, RuntimeStatus={runtimeStatus}", Status.Failed, runtimeStatus);
        }

        public static void LogTimeOffReasonError(this ILogger log, Exception ex, TeamActivityModel activityModel, TimeOffReasonModel timeOffReason)
        {
            log.LogError(EventIds.TimeOffReason, ex, "TimeOffReason: Status={status}, SourceId={sourceId}, Reason={timeOffReason}, TeamId={teamId}, Year={year}", Status.Failed, timeOffReason.WfmTimeOffReasonId, timeOffReason.Reason, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogTimeOffSkipped(this ILogger log, TeamActivityModel activityModel, string operationName, TimeOffModel timeOff)
        {
            log.LogTrace(EventIds.TimeOff, "TimeOff: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, TeamId={teamId}, Year={year}", Status.Skipped, operationName, timeOff.WfmTimeOffId, timeOff.WfmEmployeeId, activityModel.TeamId, activityModel.DateValue);
        }

        public static void LogTimeOffStart(this ILogger log, int activeTeamCount)
        {
            log.LogInformation(EventIds.TimeOff, "TimeOff: Stage={stage}, ActiveTeamCount={activeTeamCount}", Stage.Start, activeTeamCount);
        }

        public static void LogUnknownStatusOrchestrator(this ILogger log, string orchestratorName, DateTime lastUpdatedTime, string teamId)
        {
            log.LogWarning(EventIds.Orchestrator, "Orchestrator: Status={status}, Name={orchestratorName}, LastUpdatedTime={lastUpdatedTime}, TeamId={teamId}", Status.Unknown, orchestratorName, lastUpdatedTime.AsDateTimeString(), teamId);
        }

        public static void LogUnsubscribeTeam(this ILogger log, string teamId)
        {
            log.LogInformation(EventIds.Team, "Team: Status={status}, TeamId={teamId}", Status.Unsubscribed, teamId);
        }

        public static void LogUpdateSchedule(this ILogger log, UpdateScheduleModel updateScheduleModel, string operationName)
        {
            log.LogInformation(EventIds.Schedule, "UpdateSchedule: TeamIds={teamIds}, UpdateAllTeams={updateAllTeams}, OperationName={operationName}", updateScheduleModel.TeamIds, updateScheduleModel.UpdateAllTeams, operationName);
        }

        private static class EventIds
        {
            public static EventId Activity = new EventId(100, "Activity");
            public static EventId ApproveSwapShiftsRequest = new EventId(101, "Approve Swap Shift Request");
            public static EventId Availability = new EventId(102, "Availability");
            public static EventId Change = new EventId(102, "Change");
            public static EventId ClearSchedule = new EventId(104, "Clear Schedule");
            public static EventId Delta = new EventId(105, "Delta");
            public static EventId Department = new EventId(106, "Department");
            public static EventId EmployeeCacheRefresh = new EventId(107, "Employee Cache Refresh");
            public static EventId Job = new EventId(108, "Job");
            public static EventId MissingTeam = new EventId(109, "Missing Team");
            public static EventId OpenShiftAssignment = new EventId(110, "Open Shift Assignment");
            public static EventId Orchestrator = new EventId(111, "Orchestrator");
            public static EventId ReviewOpenShiftRequest = new EventId(112, "Review Open Shift Request");
            public static EventId Schedule = new EventId(113, "Schedule");
            public static EventId SchedulingGroup = new EventId(114, "Scheduling Group");
            public static EventId ShareSchedule = new EventId(115, "Share Schedule");
            public static EventId Shift = new EventId(116, "Shift");
            public static EventId Source = new EventId(117, "Source");
            public static EventId Team = new EventId(118, "Team");
            public static EventId TimeOff = new EventId(119, "TimeOff");
            public static EventId TimeOffReason = new EventId(120, "Time Off Reason");
        }

        private static class Stage
        {
            public const string Applied = "Applied";
            public const string End = "End";
            public const string Full = "Full";
            public const string Partial = "Partial";
            public const string Request = "Request";
            public const string Response = "Response";
            public const string Start = "Start";
        }

        private static class Status
        {
            public const string Disabled = "Disabled";
            public const string Enabled = "Enabled";
            public const string Failed = "Failed";
            public const string Invalid = "Invalid";
            public const string NotFound = "NotFound";
            public const string Pending = "Pending";
            public const string Skipped = "Skipped";
            public const string Stalled = "Stalled";
            public const string Subscribed = "Subscribed";
            public const string Success = "Success";
            public const string Suspended = "Suspended";
            public const string Terminated = "Terminated";
            public const string Unknown = "Unknown";
            public const string Unsubscribed = "Unsubscribed";
            public const string Valid = "Valid";
        }
    }
}
