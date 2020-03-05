// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot]
    public class Request
    {
        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the EmployeeRequestMgmt.
        /// </summary>
        [XmlElement]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }
    }
}