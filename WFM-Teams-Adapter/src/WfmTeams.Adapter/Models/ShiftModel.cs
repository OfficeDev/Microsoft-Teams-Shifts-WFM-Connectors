// ---------------------------------------------------------------------------
// <copyright file="ShiftModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ShiftModel : IDeltaItem
    {
        public ShiftModel(string wfmShiftId)
        {
            WfmShiftId = wfmShiftId ?? throw new ArgumentNullException(nameof(wfmShiftId));
        }

        public List<ActivityModel> Activities { get; set; } = new List<ActivityModel>();
        public string DepartmentName { get; set; }
        public DateTime EndDate { get; set; }
        public string WfmEmployeeId { get; set; }
        public string WfmEmployeeName { get; set; }

        [JsonIgnore]
        public string WfmId => WfmShiftId;

        public string WfmJobId { get; set; }
        public string WfmJobName { get; set; }
        public string WfmShiftId { get; set; }
        public List<ActivityModel> Jobs { get; set; } = new List<ActivityModel>();
        public DateTime StartDate { get; set; }
        public string TeamsEmployeeId { get; set; }
        public string TeamsSchedulingGroupId { get; set; }
        public string TeamsShiftId { get; set; }
        public string ThemeCode { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
