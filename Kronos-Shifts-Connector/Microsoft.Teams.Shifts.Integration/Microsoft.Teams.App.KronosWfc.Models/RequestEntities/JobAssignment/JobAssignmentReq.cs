// <copyright file="JobAssignmentReq.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the JobAssignment.
    /// </summary>
    public class JobAssignmentReq
    {
        /// <summary>
        /// Gets or sets the Identity.
        /// </summary>
        [XmlElement("Identity")]
        public Identity Ident { get; set; }
    }
}