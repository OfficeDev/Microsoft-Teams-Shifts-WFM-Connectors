// ---------------------------------------------------------------------------
// <copyright file="FeatureOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    public class FeatureOptions
    {
        public bool EnableAvailabilitySync { get; set; }
        public bool EnableOpenShiftSync { get; set; }
        public bool EnableShiftSwapAutoApproval { get; set; }
        public bool EnableOpenShiftAutoApproval { get; set; }
        public bool EnableTimeOffSync { get; set; }
        public bool EnableEmployeeTokenRefresh { get; set; }
        public bool EnableShiftSync { get; set; } = true;
    }
}
