// <copyright file="JobAssignmentDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the JobAssignmentDetails.
    /// </summary>
    public class JobAssignmentDetails
    {
        /// <summary>
        /// Gets or sets the PayRuleName.
        /// </summary>
        [XmlAttribute]
        public string PayRuleName { get; set; }

        /// <summary>
        /// Gets or sets the SupervisorPersonNumber.
        /// </summary>
        [XmlAttribute]
        public string SupervisorPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the SupervisorName.
        /// </summary>
        [XmlAttribute]
        public string SupervisorName { get; set; }

        /// <summary>
        /// Gets or sets the TimeZoneName.
        /// </summary>
        [XmlAttribute]
        public string TimeZoneName { get; set; }

        /// <summary>
        /// Gets or sets the BaseWageHourly.
        /// </summary>
        [XmlAttribute]
        public string BaseWageHourly { get; set; }
    }
}