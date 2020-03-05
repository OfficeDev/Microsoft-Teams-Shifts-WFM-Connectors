// <copyright file="JobAssignmentDetailsData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the JobAssignmentDetailsData.
    /// </summary>
    public class JobAssignmentDetailsData
    {
        /// <summary>
        /// Gets or sets the JobAssignmentDetails.
        /// </summary>
        [XmlElement("JobAssignmentDetails")]
        public JobAssignmentDetails JobAssignDet { get; set; }
    }
}