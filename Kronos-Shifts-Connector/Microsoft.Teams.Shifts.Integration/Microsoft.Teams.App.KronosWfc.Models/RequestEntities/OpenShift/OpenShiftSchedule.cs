// <copyright file="OpenShiftSchedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift
{
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule;

    /// <summary>
    /// This class models the OpenShiftSchedule.
    /// </summary>
    public class OpenShiftSchedule
    {
        /// <summary>
        /// Gets or sets the scheduleItems.
        /// </summary>
        public ScheduleItems ScheduleItems { get; set; }

        /// <summary>
        /// Gets or sets the employee.
        /// </summary>
        public Employees Employee { get; set; }

        /// <summary>
        /// Gets or sets the queryDateSpan.
        /// </summary>
        public string QueryDateSpan { get; set; }
    }
}