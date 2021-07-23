// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ScheduleShift.
    /// </summary>
    public class ScheduleShift
    {
        // [XmlArray]

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PersonIdentity> Employee { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public Common.ShiftSegments ShiftSegments { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the LockedFlag.
        /// </summary>
        [XmlAttribute]
        public string LockedFlag { get; set; }

        /// <summary>
        /// Gets or sets the ShiftLabel.
        /// </summary>
        [XmlAttribute("Shiftlabel")]
        public string ShiftLabel { get; set; }

        /// <summary>
        /// Gets or sets the StartDate.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the IsDeleted.
        /// </summary>
        [XmlAttribute]
        public string IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the shift is an open shift.
        /// </summary>
        [XmlAttribute]
        public bool IsOpenShift { get; set; }
    }
}