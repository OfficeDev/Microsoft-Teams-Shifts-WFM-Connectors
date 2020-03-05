// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Submit request.
    /// </summary>
    [XmlRoot]
    public class Request
    {
        /// <summary>
        /// Gets or sets Employee submit requests.
        /// </summary>
        [XmlElement("EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgm { get; set; }

        /// <summary>
        /// Gets or sets Action associated with request.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}
