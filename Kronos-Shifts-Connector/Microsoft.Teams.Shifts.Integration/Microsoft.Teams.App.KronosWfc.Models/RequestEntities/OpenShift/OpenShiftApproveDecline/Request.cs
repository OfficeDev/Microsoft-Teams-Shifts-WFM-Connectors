// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestManagement.
    /// </summary>
    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot]
    public class Request
    {
        /// <summary>
        /// Gets or sets the RequestMgmt.
        /// </summary>
        [XmlElement("RequestMgmt")]
        public RequestMgmt RequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}