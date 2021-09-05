// ---------------------------------------------------------------------------
// <copyright file="ConnectorOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Options
{
    using System;

    public class ConnectorOptions
    {
        public int BatchDelayMs { get; set; } = 1000;
        public bool DraftOpenShiftDeletesEnabled { get; set; } = true;
        public bool DraftShiftDeletesEnabled { get; set; }
        public bool DraftShiftsEnabled { get; set; } = true;
        public bool DraftTimeOffDeletesEnabled { get; set; }
        public string DefaultWfmTimeZone { get; set; } = "GMT Standard Time";
        public int LongOperationMaxAttempts { get; set; } = 8;
        public int LongOperationRetryIntervalSeconds { get; set; } = 15;
        public int MaximumUsers { get; set; } = 100;
        public bool OfferShiftRequestsEnabled { get; set; } = true;
        public bool OpenShiftsEnabled { get; set; } = true;
        public int RetryIntervalSeconds { get; set; } = 5;
        public int RetryMaxAttempts { get; set; } = 5;
        public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Monday;
        public int StopOrchestratorWaitSeconds { get; set; } = 10;
        public int StorageLeaseTimeSeconds { get; set; } = 15; // min 15s, max 60s
        public bool SwapShiftsRequestsEnabled { get; set; } = true;
        public string TenantId { get; set; }
        public bool TimeClockEnabled { get; set; }
        public bool TimeOffRequestsEnabled { get; set; } = true;
        public ProviderType WfmProvider { get; set; } = ProviderType.BlueYonder;
        public string GraphApiUserId { get; set; }
    }
}
