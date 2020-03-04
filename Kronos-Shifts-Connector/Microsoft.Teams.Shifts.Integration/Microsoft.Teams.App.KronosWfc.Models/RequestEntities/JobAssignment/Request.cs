// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models a request.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the JobAssignment.
        /// </summary>
        [XmlElement("JobAssignment")]
        public JobAssignmentReq JobAssign { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute("Action")]
        public string Action { get; set; }
    }
}