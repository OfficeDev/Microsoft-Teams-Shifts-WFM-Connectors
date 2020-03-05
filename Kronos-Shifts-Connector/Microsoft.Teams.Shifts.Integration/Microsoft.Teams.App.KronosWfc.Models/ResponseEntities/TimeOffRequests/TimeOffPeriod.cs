// <copyright file="TimeOffPeriod.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeOffPeriod.
    /// </summary>
    [XmlRoot(ElementName = "TimeOffPeriod")]
    public class TimeOffPeriod
    {
        /// <summary>
        /// Gets or sets the StartDate.
        /// </summary>
        [XmlAttribute(AttributeName = "StartDate")]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the Duration.
        /// </summary>
        [XmlAttribute(AttributeName = "Duration")]
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the EndDate.
        /// </summary>
        [XmlAttribute(AttributeName = "EndDate")]
        public string EndDate { get; set; }

        /// <summary>
        /// Gets or sets the PayCodeName.
        /// </summary>
        [XmlAttribute(AttributeName = "PayCodeName")]
        public string PayCodeName { get; set; }

        /// <summary>
        /// Gets or sets the Length.
        /// </summary>
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }

        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        [XmlAttribute(AttributeName = "StartTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the Sdt.
        /// </summary>
        public DateTime Sdt { get; set; }
    }
}