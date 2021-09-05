// ---------------------------------------------------------------------------
// <copyright file="ShiftSwapApprovalStatus.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Models
{
    public static class ShiftSwapApprovalStatus
    {
        public static readonly string ShiftSwapCancelled = "c";
        public static readonly string ShiftSwapManagerApproved = "a";
        public static readonly string ShiftSwapManagerDenied = "x";
        public static readonly string ShiftSwapManagerWaiting = "w";
        public static readonly string ShiftSwapRequested = "r";

        // TODO: we need to find out what i means - I have added it to this list because I have seen it at some point
        public static readonly string ShiftSwapUnknown = "i";

        public static readonly string ShiftSwapUserApproved = "y";
        public static readonly string ShiftSwapUserDenied = "n";
    }
}
