// <copyright file="TimeOffPeriod.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models time off period.
    /// </summary>
    public class TimeOffPeriod
    {
        /// <summary>
        /// Gets or sets time off start date.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets time off end date.
        /// </summary>
        [XmlAttribute]
        public string EndDate { get; set; }

        /// <summary>
        /// Gets or sets time off duration.
        /// </summary>
        [XmlAttribute]
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets time off pay code name.
        /// </summary>
        [XmlAttribute]
        public string PayCodeName { get; set; }

        /// <summary>
        /// Gets or sets time off start time.
        /// </summary>
        [XmlAttribute]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets length of time off.
        /// </summary>
        [XmlAttribute]
        public string Length { get; set; }
    }
}
