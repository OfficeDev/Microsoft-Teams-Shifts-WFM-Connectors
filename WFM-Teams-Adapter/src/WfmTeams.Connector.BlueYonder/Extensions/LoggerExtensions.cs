// ---------------------------------------------------------------------------
// <copyright file="LoggerExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Extensions
{
    using System;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Connector.BlueYonder.Exceptions;

    public static class LoggerExtensions
    {
        public static void LogBlueYonderUnauthorizedAccessException(this ILogger log, BlueYonderUnauthorizedAccessException ex, string teamId, string contextMessage = "")
        {
            log.LogError(EventIds.BlueYonderUnauthorizedAccessException, ex, "BlueYonderUnauthorizedAccessException: RequestUrl={requestUrl}, RequestHeaders={requestHeaders}, ResponseStatusCode={responseStatusCode}, ResponseContent={responseContent}, ResponseHeaders={responseHeaders}, TeamId={teamId}, ContextMessage={contextMessage}", ex.RequestUrl, ex.RequestHeaders, ex.ResponseStatusCode, ex.ResponseContent, ex.ResponseHeaders, teamId, contextMessage);
        }

        public static void LogBlueYonderGeneralException(this ILogger log, Exception ex, string buId, string entityId, string methodName)
        {
            log.LogError(EventIds.BlueYonderGeneralException, ex, "BlueYonderGeneralException: MethodName={methodName}, BusinessUnitId={buId}, WfmEntityId={wfmEntityId}", methodName, buId, entityId);
        }

        public static void LogTokenError(this ILogger log, Exception ex, string message, string token)
        {
            log.LogError(EventIds.Token, ex, "Token: Status={status}, Message={message}", Status.Invalid, message);
            log.LogTrace(EventIds.Token, ex, "Token: Status={status}, Token={token}", Status.Invalid, token);
        }

        public static void LogTokenError(this ILogger log, string message)
        {
            log.LogError(EventIds.Token, "Token: Status={status}, Message={message}", Status.Invalid, message);
        }

        public static void LogTokenSuccess(this ILogger log, string userId)
        {
            log.LogInformation(EventIds.Token, "Token: Status={status}, UserId={userId}", Status.Valid, userId);
        }

        public static void LogShiftPreferenceChangeError(this ILogger log, Exception ex, EmployeeAvailabilityModel availabilityModel, string wfmErrorCode = "")
        {
            log.LogError(EventIds.Change, ex, "Change: Status={status} WfmId={wfmId}, WfmEmployeeId={wfmEmployeeId}, WfmErrorCode={wfmErrorCode}", Status.Failed, availabilityModel.WfmId, availabilityModel.WfmEmployeeId, wfmErrorCode);
        }

        public static void LogEmployeeTokenFailure(this ILogger log, string employeeId, string errorCode, string userMessage)
        {
            log.LogError(EventIds.EmployeeTokenRefresh, "EmployeeTokenRefresh: Status={status}, WfmEmployeeId={wfmEmployeeId}, WfmErrorCode={wfrmErrorCode}, UserMessage={userMessage}", Status.Failed, employeeId, errorCode, userMessage);
        }

        public static void LogEmployeeTokenRefreshError(this ILogger log, Exception ex, string message)
        {
            log.LogError(EventIds.EmployeeTokenRefresh, ex, "EmployeeTokenRefresh: Status={status}, Message={message}", Status.Failed, message);
        }

        public static void LogTimeZoneError(this ILogger log, Exception ex, int buId, int timeZoneId, string timeZoneName)
        {
            log.LogError(EventIds.TimeZone, "TimeZoneError: BusinessUnit={buId}, TimeZoneId={timeZoneId}, TimeZoneName={timeZoneName}", buId, timeZoneId, timeZoneName);
        }

        /// <summary>
        /// Defines the EventIds logged by this WFM Connector.
        /// </summary>
        /// <remarks>ID values should start at 1000</remarks>
        private static class EventIds
        {
            public static EventId BlueYonderUnauthorizedAccessException = new EventId(1000, "Blue Yonder Unauthorized Access Exception");
            public static EventId BlueYonderGeneralException = new EventId(1001, "Blue Yonder General Exception");
            public static EventId Token = new EventId(1002, "Token");
            public static EventId Change = new EventId(1003, "Change");
            public static EventId EmployeeTokenRefresh = new EventId(1004, "Employee Token Refresh");
            public static EventId TimeZone = new EventId(1005, "TimeZone");
        }

        private static class Status
        {
            public const string Failed = "Failed";
            public const string Valid = "Valid";
            public const string Invalid = "Invalid";
        }
    }
}
