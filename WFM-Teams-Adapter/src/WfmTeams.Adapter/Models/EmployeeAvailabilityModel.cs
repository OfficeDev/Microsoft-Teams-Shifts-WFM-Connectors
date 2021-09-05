// ---------------------------------------------------------------------------
// <copyright file="EmployeeAvailabilityModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the employee availability model.
    /// </summary>
    public class EmployeeAvailabilityModel : IDeltaItem
    {
        public List<AvailabilityModel> Availability { get; set; } = new List<AvailabilityModel>();
        public DateTime CycleBaseDate { get; set; }

        public DateTime? EndDate { get; set; }
        public string WfmEmployeeId { get; set; }

        [JsonIgnore]
        public string WfmId => WfmEmployeeId;

        // ensure that the number of weeks always has a valid value as zero is not valid
        public int NumberOfWeeks { get; set; } = 1;

        public DateTime StartDate { get; set; }
        public string TeamsEmployeeId { get; set; }
        public string TimeZoneInfoId { get; set; }
    }
}
