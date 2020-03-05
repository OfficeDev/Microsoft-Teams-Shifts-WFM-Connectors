// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.BatchRequest
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class Request
    {
        /// <summary>
        /// Gets or sets the list of schedule.
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ScheduleOS> Schedule { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}