// <copyright file="ScheduleShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the ScheduleShift from Kronos.
    /// </summary>
    public class ScheduleShift
    {
        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegments")]
        public ShiftSegments ShiftSegments { get; set; }

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
        /// Gets or sets the IsDeleted.
        /// </summary>
        [XmlAttribute(AttributeName = "IsDeleted")]
        public string IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the IsOpenShift.
        /// </summary>
        [XmlAttribute(AttributeName = "IsOpenShift")]
        public string IsOpenShift { get; set; }
    }
}
