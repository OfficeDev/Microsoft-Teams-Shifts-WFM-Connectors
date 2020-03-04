// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Requests during Shift to Kronos flow.
    /// </summary>
    [XmlRoot]
    public class Request
    {
        /// <summary>
        /// Gets or sets the Employee related requests.
        /// </summary>
        [XmlElement("EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgm { get; set; }

        /// <summary>
        /// Gets or sets the Request Action to be passed to Kronos.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}
