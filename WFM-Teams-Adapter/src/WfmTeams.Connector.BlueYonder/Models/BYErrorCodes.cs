// ---------------------------------------------------------------------------
// <copyright file="BYErrorCodes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Models
{
    public static class BYErrorCodes
    {
        public const string AvailabilityCycleBaseDateMustBeLessOrEqualStartDate = nameof(AvailabilityCycleBaseDateMustBeLessOrEqualStartDate);
        public const string AvailabilityEndMustBeAfterStart = nameof(AvailabilityEndMustBeAfterStart);
        public const string AvailabilityNoChange = nameof(AvailabilityNoChange);
        public const string AvailabilityNumWeeksMustBeGreaterThanZero = nameof(AvailabilityNumWeeksMustBeGreaterThanZero);
        public const string AvailabilityPreferenceMustBeInGeneral = nameof(AvailabilityPreferenceMustBeInGeneral);
        public const string AvailabilityTimeRangesMustBeInFifteenMinIncrements = nameof(AvailabilityTimeRangesMustBeInFifteenMinIncrements);
        public const string AvailabilityWeekNumberGreaterThanCycleLength = nameof(AvailabilityWeekNumberGreaterThanCycleLength);
        public const string AvailabilityWeekNumberLessThanOne = nameof(AvailabilityWeekNumberLessThanOne);
        public const string InternalError = nameof(InternalError);
        public const string NoManagerApprovalPending = nameof(NoManagerApprovalPending);
        public const string NoOpenShiftsFound = nameof(NoOpenShiftsFound);
        public const string OpenShiftChangeRequestNotFound = nameof(OpenShiftChangeRequestNotFound);
        public const string RecipientShiftNotFound = nameof(RecipientShiftNotFound);
        public const string RequestExpired = nameof(RequestExpired);
        public const string RequestInProgress = nameof(RequestInProgress);
        public const string ScheduledShiftCannotOverlap = nameof(ScheduledShiftCannotOverlap);
        public const string SenderShiftNotFound = nameof(SenderShiftNotFound);
        public const string ShiftNotAvailableToUser = nameof(ShiftNotAvailableToUser);
        public const string SwapRequestNotFound = nameof(SwapRequestNotFound);
        public const string UnknownErrorCode = nameof(UnknownErrorCode);
        public const string UnsupportedOperation = nameof(UnsupportedOperation);
        public const string UserCredentialsNotFound = nameof(UserCredentialsNotFound);
        public const string UserUnauthorized = nameof(UserUnauthorized);
    }
}
