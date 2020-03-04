// <copyright file="ScheduleItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts
{
    using System.Xml.Serialization;

    /// <summary>
    /// Gets or sets the scheduleItems.
    /// </summary>
    public class ScheduleItems
    {
        /// <summary>
        /// Gets or sets the scheduleShift.
        /// </summary>
        [XmlElement("ScheduleShift", typeof(ScheduleShift))]
#pragma warning disable CA1819 // Properties should not return arrays
        public ScheduleShift[] ScheduleShift { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the schedulePayCodeEdit.
        /// </summary>
        [XmlElement("SchedulePayCodeEdit", typeof(SchedulePayCodeEdit))]
#pragma warning disable CA1819 // Properties should not return arrays
        public SchedulePayCodeEdit[] SchedulePayCodeEdit { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}