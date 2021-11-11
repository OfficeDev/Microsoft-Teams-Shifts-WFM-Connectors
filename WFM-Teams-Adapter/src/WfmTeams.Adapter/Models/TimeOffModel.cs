// ---------------------------------------------------------------------------
// <copyright file="TimeOffModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;
    using Newtonsoft.Json;

    public class TimeOffModel : IDeltaItem
    {
        public DateTime EndDate { get; set; }
        public string WfmEmployeeId { get; set; }

        [JsonIgnore]
        public string WfmId => WfmTimeOffId;

        public string WfmTimeOffId { get; set; }
        public string WfmTimeOffReason { get; set; }
        public string WfmTimeOffTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public string TeamsEmployeeId { get; set; }
        public string TeamsTimeOffId { get; set; }
        public string TeamsTimeOffReasonId { get; set; }
    }
}
