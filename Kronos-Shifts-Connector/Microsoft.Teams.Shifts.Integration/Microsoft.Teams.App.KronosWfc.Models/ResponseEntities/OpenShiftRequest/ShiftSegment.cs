// <copyright file="ShiftSegment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftSegment.
    /// </summary>
    [XmlRoot(ElementName = "ShiftSegment")]
    public class ShiftSegment
    {
        /// <summary>
        /// Gets or sets the SegmentTypeName.
        /// </summary>
        [XmlAttribute(AttributeName = "SegmentTypeName")]
        public string SegmentTypeName { get; set; }

        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        [XmlAttribute(AttributeName = "StartTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the StartDayNumber.
        /// </summary>
        [XmlAttribute(AttributeName = "StartDayNumber")]
        public string StartDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the EndTime.
        /// </summary>
        [XmlAttribute(AttributeName = "EndTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDayNumber.
        /// </summary>
        [XmlAttribute(AttributeName = "EndDayNumber")]
        public string EndDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute(AttributeName = "OrgJobPath")]
        public string OrgJobPath { get; set; }
    }
}