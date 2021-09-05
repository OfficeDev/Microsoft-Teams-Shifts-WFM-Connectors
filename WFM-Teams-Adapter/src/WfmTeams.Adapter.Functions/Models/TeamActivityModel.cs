// ---------------------------------------------------------------------------
// <copyright file="TeamActivityModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System;

    public class TeamActivityModel
    {
        public string ActivityType { get; set; }
        public string DateValue { get; set; }
        public DateTime StartDate { get; set; }
        public string WfmBuId { get; set; }
        public string TeamId { get; set; }
        public string TimeZoneInfoId { get; set; }
    }
}
