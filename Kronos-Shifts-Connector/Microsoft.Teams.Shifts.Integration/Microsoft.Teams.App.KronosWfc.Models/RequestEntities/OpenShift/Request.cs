// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models a request.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
        public ScheduleOS Schedule { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}
