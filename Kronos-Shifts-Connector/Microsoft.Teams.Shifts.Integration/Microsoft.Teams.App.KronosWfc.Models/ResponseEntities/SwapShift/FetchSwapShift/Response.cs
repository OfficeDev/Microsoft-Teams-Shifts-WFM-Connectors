// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Response.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the EmployeeRequestMgmt.
        /// </summary>
        [XmlElement(ElementName = "RequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }

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
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
    }
}