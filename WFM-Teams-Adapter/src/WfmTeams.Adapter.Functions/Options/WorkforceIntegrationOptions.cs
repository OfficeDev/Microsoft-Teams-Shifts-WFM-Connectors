// ---------------------------------------------------------------------------
// <copyright file="WorkforceIntegrationOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    using WfmTeams.Adapter.Options;

    public class WorkforceIntegrationOptions : ConnectorOptions
    {
        public int ApiVersion { get; set; } = 1;
        public string EligibilityFilteringEnabledEntities { get; set; } = "swapRequest";
        public string WorkforceIntegrationDisplayName { get; set; }
        public string WorkforceIntegrationId { get; set; }
        public string WorkforceIntegrationSecret { get; set; }
        public string WorkforceIntegrationSupportedEntities { get; set; } = "Shift, SwapRequest, OpenShift, OpenShiftRequest, UserShiftPreferences";
    }
}
