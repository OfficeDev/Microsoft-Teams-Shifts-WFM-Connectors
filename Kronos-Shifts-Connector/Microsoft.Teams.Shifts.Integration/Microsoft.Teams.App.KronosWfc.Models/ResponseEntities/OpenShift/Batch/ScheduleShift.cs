// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.Batch
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData;

    /// <summary>
    /// This class models the Schedule Shift.
    /// </summary>
    [XmlRoot(ElementName = "ScheduleShift")]
    public class ScheduleShift
    {
        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegments")]
        public ShiftSegments ShiftSegments { get; set; }

        /// <summary>
        /// Gets or sets the open shift comments.
        /// </summary>
        [XmlElement(ElementName = "Comments")]
        public Comments OpenShiftComments { get; set; }

        /// <summary>
        /// Gets or sets the LockedFlag.
        /// </summary>
        [XmlAttribute(AttributeName = "LockedFlag")]
        public string LockedFlag { get; set; }

        /// <summary>
        /// Gets or sets the StartDate.
        /// </summary>
        [XmlAttribute(AttributeName = "StartDate")]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether or not the shift is deleted.
        /// </summary>
        [XmlAttribute(AttributeName = "IsDeleted")]
        public string IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether or not the shift is an open shift.
        /// </summary>
        [XmlAttribute(AttributeName = "IsOpenShift")]
        public string IsOpenShift { get; set; }
    }
}