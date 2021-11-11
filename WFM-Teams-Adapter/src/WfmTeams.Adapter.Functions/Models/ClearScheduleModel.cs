// ---------------------------------------------------------------------------
// <copyright file="ClearScheduleModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ClearScheduleModel
    {
        public bool ClearOpenShifts { get; set; } = true;

        public bool ClearSchedulingGroups { get; set; } = true;

        public bool ClearShifts { get; set; } = true;

        public bool ClearTimeOff { get; set; } = true;

        public DateTime EndDate { get; set; }

        public int? FutureWeeks { get; set; }

        public int? PastWeeks { get; set; }

        public DateTime? QueryEndDate { get; set; }

        public DateTime StartDate { get; set; }

        [Required]
        public string TeamId { get; set; }

        public DateTime UtcEndDate { get; set; }
        public DateTime UtcStartDate { get; set; }
    }
}
