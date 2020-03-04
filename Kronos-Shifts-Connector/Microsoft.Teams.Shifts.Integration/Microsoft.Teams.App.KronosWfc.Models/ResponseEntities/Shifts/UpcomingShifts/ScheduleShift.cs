// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData;

    /// <summary>
    /// This class models the scheduleShift.
    /// </summary>
    public class ScheduleShift
    {
        /// <summary>
        /// Gets or sets the employee.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PersonIdentity> Employee { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        [XmlElement(ElementName = "Comments")]
        public Comments ShiftComments { get; set; }

        /// <summary>
        /// Gets or sets the shift segments.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftSegment> ShiftSegments { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the lockedFlag.
        /// </summary>
        [XmlAttribute]
        public string LockedFlag { get; set; }

        /// <summary>
        /// Gets or sets the startDate.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether or not something isDeleted.
        /// </summary>
        [XmlAttribute]
        public string IsDeleted { get; set; }
    }
}