// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Schedule
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [XmlElement]
        public ScheduleRes Schedule { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Jsession.
        /// </summary>
        [XmlIgnore]
        public string Jsession { get; set; }

        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        public Error Error { get; set; }
    }
}