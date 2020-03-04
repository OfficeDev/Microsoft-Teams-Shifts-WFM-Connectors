// <copyright file="ShiftSegment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftSegment.
    /// </summary>
    public class ShiftSegment
    {
        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        [XmlAttribute]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the EndTime.
        /// </summary>
        [XmlAttribute]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the EndDayNumber.
        /// </summary>
        [XmlAttribute]
        public string EndDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the SegmentTypeName.
        /// </summary>
        [XmlAttribute]
        public string SegmentTypeName { get; set; }
    }
}