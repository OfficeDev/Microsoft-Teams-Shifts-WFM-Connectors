// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the necessary response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the JobAssignment.
        /// </summary>
        [XmlElement("JobAssignment")]
        public JobAssignmentRes JobAssign { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
    }
}