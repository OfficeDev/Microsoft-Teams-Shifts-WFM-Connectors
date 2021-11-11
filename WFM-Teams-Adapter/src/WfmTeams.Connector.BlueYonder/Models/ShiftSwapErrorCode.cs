// ---------------------------------------------------------------------------
// <copyright file="ShiftSwapErrorCode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Models
{
    public enum ShiftSwapErrorCode
    {
        UnknownValue0 = 0,
        UnknownValue1 = 1,
        EmployeeCannotWorkShift = 2,
        OriginalShiftIsNotAValidShift = 3,
        OriginalShiftDoesNotBelongToEmployee = 4,
        OriginalShiftIsNotInAvailableSwapWindow = 5
    }
}