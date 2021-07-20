// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Schedule Shifts.
    /// </summary>
    public class ScheduleShift
    {
        /// <summary>
        /// Gets or sets the StartDate of the shift.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the ShiftLabel.
        /// </summary>
        [XmlAttribute("Shiftlabel")]
        public string ShiftLabel { get; set; }

        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
        [XmlElement]
        public List<Employee> Employee { get; set; }

        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement]
        public ShiftSegments ShiftSegments { get; set; }
    }
}