// ---------------------------------------------------------------------------
// <copyright file="WfmOpenShiftRequestModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    public class WfmOpenShiftRequestModel
    {
        public string BuId { get; set; }
        public DateTime? ManagerDateTime { get; set; }
        public string ManagerId { get; set; }
        public string ManagerLoginName { get; set; }
        public string ManagerMessage { get; set; }
        public DateTime? SenderDateTime { get; set; }
        public string SenderId { get; set; }
        public string SenderLoginName { get; set; }
        public string SenderMessage { get; set; }
        public string TimeZoneInfoId { get; set; }
        public string OpenShiftId { get; set; }
        public ShiftModel WfmOpenShift { get; set; }
    }
}
