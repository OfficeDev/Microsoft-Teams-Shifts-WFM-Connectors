// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the EmployeeRequestMgmt.
        /// </summary>
        [XmlElement("EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgm { get; set; }

        /// <summary>
        /// Gets or sets the status.
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