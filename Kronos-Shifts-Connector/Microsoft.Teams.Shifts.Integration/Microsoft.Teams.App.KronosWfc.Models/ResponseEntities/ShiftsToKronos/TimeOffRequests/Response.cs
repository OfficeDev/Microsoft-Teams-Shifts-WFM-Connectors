// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Response.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the Employee Request Management.
        /// </summary>
        [XmlElement("EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgm { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Sequence.
        /// </summary>
        [XmlAttribute]
        public string Sequence { get; set; }

        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        public Error Error { get; set; }
    }
}
