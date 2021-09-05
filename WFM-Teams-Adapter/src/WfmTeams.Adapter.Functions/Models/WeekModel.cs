// ---------------------------------------------------------------------------
// <copyright file="WeekModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System;
    using System.Collections.Generic;

    public class WeekModel
    {
        public DateTime StartDate { get; set; }
        public string WfmBuId { get; set; }
        public string TeamId { get; set; }
        public string TimeZoneInfoId { get; set; }

        public IDictionary<string, object> AsDimensions(string shiftType)
        {
            return new Dictionary<string, object>
            {
                { nameof(TeamId), TeamId },
                { nameof(StartDate), StartDate },
                { "ShiftType", shiftType }
            };
        }
    }
}
