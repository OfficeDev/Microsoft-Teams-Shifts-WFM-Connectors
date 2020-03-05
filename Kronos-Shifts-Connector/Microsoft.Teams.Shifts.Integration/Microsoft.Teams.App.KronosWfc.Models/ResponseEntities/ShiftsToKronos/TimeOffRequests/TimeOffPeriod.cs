// <copyright file="TimeOffPeriod.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeOffPeriod.
    /// </summary>
    public class TimeOffPeriod
    {
        /// <summary>
        /// Gets or sets the Start Date.
        /// </summary>
        [XmlAttribute("StartDate")]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the Duration.
        /// </summary>
        [XmlAttribute("Duration")]
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the End Date.
        /// </summary>
        [XmlAttribute("EndDate")]
        public string EndDate { get; set; }

        /// <summary>
        /// Gets or sets the Pay code name.
        /// </summary>
        [XmlAttribute("PayCodeName")]
        public string PayCodeName { get; set; }

        /// <summary>
        /// Gets or sets the Start Time.
        /// </summary>
        [XmlAttribute]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the Length.
        /// </summary>
        [XmlAttribute]
        public string Length { get; set; }

        /// <summary>
        /// Gets or sets the Date.
        /// </summary>
        public DateTime Sdt { get; set; }

        /// <summary>
        /// Gets or sets the Date.
        /// </summary>
        public DateTime Edt { get; set; }
    }
}
