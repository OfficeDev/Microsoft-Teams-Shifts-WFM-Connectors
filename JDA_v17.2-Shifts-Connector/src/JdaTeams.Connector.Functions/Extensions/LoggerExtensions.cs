using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.MicrosoftGraph.Exceptions;
using JdaTeams.Connector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Collections.Generic;

namespace JdaTeams.Connector.Functions.Extensions
{
    public static class LoggerExtensions
    {
        public static void LogShifts(this ILogger log, WeekModel week, List<ShiftModel> shifts)
        {
            log.LogInformation(new EventId(2, "Source"), "Source: Shifts={shiftCount}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", shifts.Count, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogDelta(this ILogger log, string stage, WeekModel week, DeltaModel delta)
        {
            log.LogInformation(new EventId(3, "Delta"), "Delta: Stage={stage}, Created={createdCount}, Updated={updatedCount}, Deleted={deletedCount}, Failed={failedCount}, Skipped={skippedCount}, TeamId={teamId}, WeekDate={weekDate}", stage, delta.Created.Count, delta.Updated.Count, delta.Deleted.Count, delta.Failed.Count, delta.Skipped.Count, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogFullDelta(this ILogger log, WeekModel week, DeltaModel delta)
        {
            log.LogDelta(Stage.Full, week, delta);
        }

        public static void LogPartialDelta(this ILogger log, WeekModel week, DeltaModel delta)
        {
            log.LogDelta(Stage.Partial, week, delta);
        }

        public static void LogAppliedDelta(this ILogger log, WeekModel week, DeltaModel delta)
        {
            log.LogDelta(Stage.Applied, week, delta);
        }

        public static void LogSchedule(this ILogger log, TeamModel team, ScheduleModel schedule)
        {
            log.LogInformation(new EventId(4, "Schedule"), "Schedule: Status={status}, IsEnabled={isEnabled}, TimeZone={timeZone}, TeamId={teamId}", schedule.Status, schedule.IsEnabled, schedule.TimeZone, team.TeamId);
        }

        public static void LogEmployeeNotFound(this ILogger log, WeekModel week, ShiftModel shift)
        {
            log.LogWarning(new EventId(5, "Employee"), "Employee: Status={status}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.NotFound, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogJobNotFound(this ILogger log, WeekModel week, ActivityModel job)
        {
            log.LogWarning(new EventId(6, "Job"), "Job: Status={status}, JobId={jobId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.NotFound, job.JdaJobId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogDepartmentNotFound(this ILogger log, WeekModel week, ShiftModel shift)
        {
            log.LogWarning(new EventId(7, "Department"), "Department: Status={status}, JobId={jobId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.NotFound, shift.JdaJobId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogMemberError(this ILogger log, Exception ex, WeekModel week, EmployeeModel employee)
        {
            log.LogError(new EventId(8, "Member"), ex, "Member: Status={status}, EmployeeId={employeeId}, LoginName={loginName}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, employee.SourceId, employee.LoginName, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogShiftError(this ILogger log, Exception ex, WeekModel week, string operationName, ShiftModel shift)
        {
            log.LogError(new EventId(9, "Shift"), ex, "Shift: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, operationName, shift.JdaShiftId, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogShiftError(this ILogger log, MicrosoftGraphException ex, WeekModel week, string operationName, ShiftModel shift)
        {
            log.LogError(new EventId(9, "Shift"), ex, "Shift: Status={status}, OperationName={operationName}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, SourceId={sourceId}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, operationName, ex.Error.Code, ex.Error.Message, ex.Error.InnerError?.RequestId, ex.Error.InnerError?.Date, shift.JdaShiftId, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogShiftError(this ILogger log, Exception ex, ClearScheduleModel clear, string operationName, ShiftModel shift)
        {
            log.LogError(new EventId(9, "Shift"), ex, "Shift: Status={status}, OperationName={operationName}, SourceId={sourceId}, DestinationId={destinationId}, EmployeeId={employeeId}, TeamId={teamId}, DayDate={dayDate}", Status.Failed, operationName, shift.JdaShiftId, shift.TeamsShiftId, shift.JdaEmployeeId, clear.TeamId, clear.StartDate.AsDateString());
        }

        public static void LogShiftError(this ILogger log, Exception ex, ClearScheduleModel clear, string operationName)
        {
            log.LogError(new EventId(9, "Shift"), ex, "Shift: Status={status}, OperationName={operationName}, TeamId={teamId}, DayDate={dayDate}", Status.Failed, operationName, clear.TeamId, clear.StartDate.AsDateString());
        }

        public static void LogShiftSkipped(this ILogger log, WeekModel week, string operationName, ShiftModel shift)
        {
            log.LogTrace(new EventId(10, "Shift"), "Shift: Status={status}, OperationName={operationName}, SourceId={sourceId}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Skipped, operationName, shift.JdaShiftId, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogSchedulingGroupError(this ILogger log, Exception ex, WeekModel week, ShiftModel shift)
        {
            log.LogError(new EventId(11, "Scheduling Group"), ex, "Scheduling Group: Status={status}, DepartmentName={departmentName}, SourceId={sourceId}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, shift.DepartmentName, shift.JdaShiftId, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogSchedulingGroupError(this ILogger log, MicrosoftGraphException ex, WeekModel week, ShiftModel shift)
        {
            log.LogError(new EventId(11, "Scheduling Group"), ex, "Scheduling Group: Status={status}, ErrorCode={errorCode}, ErrorDescription={errorDescription}, ErrorRequestId={errorRequestId}, ErrorDate={errorDate}, DepartmentName={departmentName}, SourceId={sourceId}, EmployeeId={employeeId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, ex.Error.Code, ex.Error.Message, ex.Error.InnerError?.RequestId, ex.Error.InnerError?.Date, shift.DepartmentName, shift.JdaShiftId, shift.JdaEmployeeId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogSchedulingGroupError(this ILogger log, Exception ex, WeekModel week, string departmentName, string schedulingGroupId)
        {
            log.LogError(new EventId(11, "Scheduling Group"), ex, "Scheduling Group: Status={status}, DepartmentName={departmentName}, SchedulingGroupId={schedulingGroupId}, StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", Status.Failed, departmentName, schedulingGroupId, week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        public static void LogClearStart(this ILogger log, ClearScheduleModel clear)
        {
            log.LogInformation(new EventId(12, "Clear"), "Clear: Stage={stage}, TeamId={teamId}, StartDate={startDate}, EndDate={endDate}", Stage.Start, clear.TeamId, clear.StartDate.AsDateString(), clear.EndDate.AsDateString());
        }

        public static void LogClearEnd(this ILogger log, ClearScheduleModel clear, ResultModel resultModel)
        {
            log.Log(resultModel.LogLevel, new EventId(12, "Clear"), "Clear: Stage={stage}, TeamId={teamId}, StartDate={startDate}, DeletedCount={deletedCount}, IterationCount={iterationCount}, IsFinished={isFinished}", Stage.End, clear.TeamId, clear.StartDate.AsDateString(), resultModel.DeletedCount, resultModel.IterationCount, resultModel.Finished);
        }

        public static void LogTeamError(this ILogger log, Exception ex, TeamModel team)
        {
            log.LogError(new EventId(13, "Team"), ex, "Team: StoreId={storeId}, TeamId={teamId}", team.StoreId, team.TeamId);
        }

        public static void LogWeek(this ILogger log, WeekModel week)
        {
            log.LogInformation(new EventId(14, "Week"), "Week: StoreId={storeId}, TeamId={teamId}, WeekDate={weekDate}", week.StoreId, week.TeamId, week.StartDate.AsDateString());
        }

        private static class Status
        {
            public const string NotFound = "NotFound";
            public const string Failed = "Failed";
            public const string Skipped = "Skipped";
        }

        private static class Stage
        {
            public const string Start = "Start";
            public const string Full = "Full";
            public const string Partial = "Partial";
            public const string Applied = "Applied";
            public const string End = "End";
        }
    }
}
