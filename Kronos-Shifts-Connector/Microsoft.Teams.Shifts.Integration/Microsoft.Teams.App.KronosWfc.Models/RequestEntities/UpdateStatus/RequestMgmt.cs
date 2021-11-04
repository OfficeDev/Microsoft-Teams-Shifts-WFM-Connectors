// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.UpdateStatus
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
    [XmlRoot(ElementName = "RequestMgmt")]
    public class RequestMgmt
    {
        /// <summary>
        /// Gets or sets Query date span.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets Employee details.
        /// </summary>
        [XmlElement("Employees")]
        public Employee Employees { get; set; }

        /// <summary>
        /// Gets or sets Swap Shift Request Ids.
        /// </summary>
        [XmlElement("RequestIds")]
        public RequestIds RequestIds { get; set; }

        /// <summary>
        /// Gets or sets Swap Shift Request status changes.
        /// </summary>
        [XmlElement("RequestStatusChanges")]
        public RequestStatusChanges RequestStatusChanges { get; set; }
    }
}