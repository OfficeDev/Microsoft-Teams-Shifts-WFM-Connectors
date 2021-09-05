// ---------------------------------------------------------------------------
// <copyright file="ActivityModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    /// <summary>
    /// Defines the model of an activity (job) within a shift or open shift.
    /// </summary>
    public class ActivityModel
    {
        public string Code { get; set; }
        public string DepartmentName { get; set; }
        public DateTime EndDate { get; set; }
        public string WfmJobId { get; set; }
        public DateTime StartDate { get; set; }
        public string ThemeCode { get; set; }
    }
}
