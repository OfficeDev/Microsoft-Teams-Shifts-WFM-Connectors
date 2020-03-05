// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Open Shift Request response.
    /// </summary>
    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        /// <summary>
        /// Gets or sets EmployeeRequestMgmt.
        /// </summary>
        [XmlElement(ElementName = "EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}