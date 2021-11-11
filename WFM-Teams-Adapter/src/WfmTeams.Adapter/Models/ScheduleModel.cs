// ---------------------------------------------------------------------------
// <copyright file="ScheduleModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;
    using System.Collections.Generic;
    using TimeZoneConverter;
    using WfmTeams.Adapter.Options;

    public class ScheduleModel
    {
        public bool IsEnabled { get; set; }

        public bool IsProvisioned => Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true
            && IsEnabled;

        public bool IsUnavailable => Status?.Equals("NotStarted", StringComparison.OrdinalIgnoreCase) == true
            || Status?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true;

        public bool OfferShiftRequestsEnabled { get; set; }
        public bool OpenShiftsEnabled { get; set; }
        public string Status { get; set; }
        public bool SwapShiftsRequestsEnabled { get; set; }
        public bool TimeClockEnabled { get; set; }
        public bool TimeOffRequestsEnabled { get; set; }
        public string TimeZone { get; set; }
        public List<string> WorkforceIntegrationIds { get; set; } = new List<string>();

        public static ScheduleModel Create(ConnectorOptions options, string workforceIntegrationId, string timeZoneInfoId)
        {
            var model = new ScheduleModel
            {
                TimeZone = TZConvert.WindowsToIana(timeZoneInfoId),
                IsEnabled = true,
                TimeClockEnabled = options.TimeClockEnabled,
                OpenShiftsEnabled = options.OpenShiftsEnabled,
                SwapShiftsRequestsEnabled = options.SwapShiftsRequestsEnabled,
                OfferShiftRequestsEnabled = options.OfferShiftRequestsEnabled,
                TimeOffRequestsEnabled = options.TimeOffRequestsEnabled
            };

            if (!string.IsNullOrEmpty(workforceIntegrationId))
            {
                model.WorkforceIntegrationIds.Add(workforceIntegrationId);
            }

            return model;
        }
    }
}
