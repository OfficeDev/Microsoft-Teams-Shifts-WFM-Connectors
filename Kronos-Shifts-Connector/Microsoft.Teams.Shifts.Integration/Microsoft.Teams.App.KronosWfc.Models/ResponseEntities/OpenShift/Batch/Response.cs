// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.Batch
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the response in batch format.
    /// </summary>
    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        /// <summary>
        /// Gets or sets the Schedules.
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<OpenShiftBatchSchedule> Schedules { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        public Error Error { get; set; }
    }
}