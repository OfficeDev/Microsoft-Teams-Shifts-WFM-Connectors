// ---------------------------------------------------------------------------
// <copyright file="ErrorCodes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.ChangeRequests
{
    public static class ErrorCodes
    {
        public const string AvailabilityCycleBaseDateMustBeLessOrEqualStartDate = nameof(AvailabilityCycleBaseDateMustBeLessOrEqualStartDate);
        public const string AvailabilityEndMustBeAfterStart = nameof(AvailabilityEndMustBeAfterStart);
        public const string AvailabilityNoChange = nameof(AvailabilityNoChange);
        public const string AvailabilityNumWeeksMustBeGreaterThanZero = nameof(AvailabilityNumWeeksMustBeGreaterThanZero);
        public const string AvailabilityPreferenceMustBeInGeneral = nameof(AvailabilityPreferenceMustBeInGeneral);
        public const string AvailabilityTimeRangesMustBeInFifteenMinIncrements = nameof(AvailabilityTimeRangesMustBeInFifteenMinIncrements);
        public const string AvailabilityUnknownErrorCode = nameof(AvailabilityUnknownErrorCode);
        public const string AvailabilityWeekNumberGreaterThanCycleLength = nameof(AvailabilityWeekNumberGreaterThanCycleLength);
        public const string AvailabilityWeekNumberLessThanOne = nameof(AvailabilityWeekNumberLessThanOne);
        public const string InternalError = nameof(InternalError);
        public const string NoOpenShiftsFound = nameof(NoOpenShiftsFound);
        public const string ChangeRequestNotFound = nameof(ChangeRequestNotFound);
        public const string RecipientShiftNotFound = nameof(RecipientShiftNotFound);
        public const string RequestExpired = nameof(RequestExpired);
        public const string RequestInProgress = nameof(RequestInProgress);
        public const string SenderShiftNotFound = nameof(SenderShiftNotFound);
        public const string ShiftNotAvailableToUser = nameof(ShiftNotAvailableToUser);
        public const string SwapRequestNotFound = nameof(SwapRequestNotFound);
        public const string UnsupportedOperation = nameof(UnsupportedOperation);
        public const string UserCredentialsNotFound = nameof(UserCredentialsNotFound);
        public const string UserUnauthorized = nameof(UserUnauthorized);
    }
}
