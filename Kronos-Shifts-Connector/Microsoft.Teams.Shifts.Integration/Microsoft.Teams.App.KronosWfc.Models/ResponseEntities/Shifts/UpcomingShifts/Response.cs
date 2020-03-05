// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the upcoming shifts response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the schedule.
        /// </summary>
        [XmlElement]
        public ScheduleUpcoming Schedule { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the jsession.
        /// </summary>
        [XmlIgnore]
        public string Jsession { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
    }
}