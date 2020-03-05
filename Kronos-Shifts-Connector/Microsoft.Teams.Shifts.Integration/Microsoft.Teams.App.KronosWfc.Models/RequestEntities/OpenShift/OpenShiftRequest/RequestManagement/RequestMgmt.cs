// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
    public class RequestMgmt
    {
        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
        [XmlElement("Employees")]
        public Employee Employees { get; set; }

        /// <summary>
        /// Gets or sets the RequestIds.
        /// </summary>
        [XmlElement("RequestIds")]
        public RequestIds RequestIds { get; set; }

        /// <summary>
        /// Gets or sets the RequestStatusChanges.
        /// </summary>
        [XmlElement("RequestStatusChanges")]
        public RequestStatusChanges RequestStatusChanges { get; set; }
    }
}