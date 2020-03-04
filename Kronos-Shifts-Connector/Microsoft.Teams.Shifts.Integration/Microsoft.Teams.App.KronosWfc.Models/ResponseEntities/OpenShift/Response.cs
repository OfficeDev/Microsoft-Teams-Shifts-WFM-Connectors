// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the OpenShift from Kronos.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
        public ScheduleOsRes Schedule { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets Error response.
        /// </summary>
        public Error Error { get; set; }
    }
}
