// <copyright file="JobAssignmentRes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the JobAssignment.
    /// </summary>
    public class JobAssignmentRes
    {
        /// <summary>
        /// Gets or sets the PrimaryLaborAccounts.
        /// </summary>
        [XmlElement("PrimaryLaborAccounts")]
        public PrimaryLaborAccounts PrimaryLaborAccList { get; set; }

        /// <summary>
        /// Gets or sets the BaseWageRates.
        /// </summary>
        [XmlElement("BaseWageRates")]
        public BaseWageRates BaseWageRats { get; set; }

        /// <summary>
        /// Gets or sets the Period.
        /// </summary>
        [XmlElement("Period")]
        public Period Perd { get; set; }

        /// <summary>
        /// Gets or sets the JobAssignmentDetails.
        /// </summary>
        [XmlElement("JobAssignmentDetailsData")]
        public JobAssignmentDetailsData JobAssignDetData { get; set; }
    }
}