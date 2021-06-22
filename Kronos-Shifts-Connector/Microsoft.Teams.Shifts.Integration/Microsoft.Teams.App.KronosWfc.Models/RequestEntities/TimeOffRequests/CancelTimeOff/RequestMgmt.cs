// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.CancelTimeOff
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
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
        /// Gets or sets the RequestIds.
        /// </summary>
        [XmlElement("RequestIds")]
        public RequestIds RequestIds { get; set; }
    }
}