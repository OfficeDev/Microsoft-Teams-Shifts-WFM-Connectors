// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline
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
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlElement("RequestFor")]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the RequestIds.
        /// </summary>
        [XmlElement("RequestIds")]
        public RequestIds RequestIds { get; set; }
    }
}