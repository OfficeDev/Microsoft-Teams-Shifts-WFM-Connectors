// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

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
        /// Gets or sets the start date of the shift we want to replace.
        /// </summary>
        [XmlAttribute]
        public string ReplaceStartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the shift we want to replace.
        /// </summary>
        [XmlAttribute]
        public string ReplaceEndDate { get; set; }

        /// <summary>
        /// Gets or sets the start time of the shift we want to replace.
        /// </summary>
        [XmlAttribute]
        public string ReplaceStartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the shift we want to replace.
        /// </summary>
        [XmlAttribute]
        public string ReplaceEndTime { get; set; }

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

        /// <summary>
        /// Gets or sets the Comments associated with time off.
        /// </summary>
        [XmlElement]
        public Comments Comments { get; set; }

        /// <summary>
        /// Denotes whether the shift label should be serialised if it is null.
        /// </summary>
        [XmlIgnore]
        public bool ShiftLabelSpecified;
    }
}