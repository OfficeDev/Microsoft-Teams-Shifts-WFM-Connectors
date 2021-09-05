// ---------------------------------------------------------------------------
// <copyright file="ScheduleRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using WfmTeams.Adapter.Models;

    public partial class ScheduleRequest
    {
        public ScheduleRequest(ScheduleModel scheduleModel)
        {
            Enabled = scheduleModel.IsEnabled;
            TimeZone = scheduleModel.TimeZone;
            TimeClockEnabled = scheduleModel.TimeClockEnabled;
            OpenShiftsEnabled = scheduleModel.OpenShiftsEnabled;
            SwapShiftsRequestsEnabled = scheduleModel.SwapShiftsRequestsEnabled;
            OfferShiftRequestsEnabled = scheduleModel.OfferShiftRequestsEnabled;
            TimeOffRequestsEnabled = scheduleModel.TimeOffRequestsEnabled;
            WorkforceIntegrationIds = scheduleModel.WorkforceIntegrationIds;
        }
    }
}
