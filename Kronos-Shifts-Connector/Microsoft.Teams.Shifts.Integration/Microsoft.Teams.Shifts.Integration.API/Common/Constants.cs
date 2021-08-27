// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    /// <summary>
    /// This class models the constants being used.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// User.Read score for Graph API.
        /// </summary>
        public const string ScopeUserRead = "User.Read";

        /// <summary>
        /// Bearer for authorization.
        /// </summary>
        public const string BearerAuthorizationScheme = "Bearer";

        /// <summary>
        /// Cache key to store Kronos JSessionId.
        /// </summary>
        public const string KronosLoginCacheKey = "KronosLoginCache";

        /// <summary>
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        public const string Resource = "https://graph.microsoft.com";

        /// <summary>
        /// The scope for WorkforceIntegration.Read.All.
        /// </summary>
        public const string ScopeWorkforceIntegrationRead = "WorkforceIntegration.Read.All";

        /// <summary>
        /// The scope for WorkforceIntegration.ReadWrite.All.
        /// </summary>
        public const string ScopeWorkforceIntegrationReadWriteAll = "WorkforceIntegration.ReadWrite.All";

        /// <summary>
        /// The scope for Group.Read.All.
        /// </summary>
        public const string ScopeGroupReadAll = "Group.Read.All";

        /// <summary>
        /// The scope for Group.ReadWrite.All.
        /// </summary>
        public const string ScopeGroupReadWriteAll = "Group.ReadWrite.All";

        /// <summary>
        /// Workforce integration supported entities.
        /// </summary>
        public const string WFISupports = "Shift, SwapRequest, OpenShift, OpenShiftRequest, TimeOffRequest";

        /// <summary>
        /// Number of open slots from Kronos.
        /// </summary>
        public const string KronosOpenShiftsSlotCount = "1";

        /// <summary>
        /// Number of open slots for creating a new Open Shift in Shifts.
        /// </summary>
        public const int ShiftsOpenSlotCount = 1;

        /// <summary>
        /// Defines the StartDayNumber string.
        /// </summary>
        public const string StartDayNumberString = "1";

        /// <summary>
        /// Defines the EndDayNumber string.
        /// </summary>
        public const string EndDayNumberString = "1";

        /// <summary>
        /// Defines the date format.
        /// </summary>
        public const string DateFormat = "M/dd/yyyy";

        /// <summary>
        /// Defines the API controller method in the ShiftController.
        /// </summary>
        public const string SyncShiftsFromKronos = "SyncShiftsFromKronos";

        /// <summary>
        /// This constant defines the pending status for the Open Shift Request from Shifts.
        /// </summary>
        public const string ShiftsOpenShiftRequestPendingStatus = "Pending";
    }
}