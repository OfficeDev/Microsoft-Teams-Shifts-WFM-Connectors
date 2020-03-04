// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
    [XmlRoot(ElementName = "RequestMgmt")]
    public class RequestMgmt
    {
        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
        [XmlElement(ElementName = "Employees")]
        public Employees Employees { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlAttribute(AttributeName = "RequestFor")]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the StatusName.
        /// </summary>
        [XmlAttribute(AttributeName = "StatusName")]
        public string StatusName { get; set; }
    }
}