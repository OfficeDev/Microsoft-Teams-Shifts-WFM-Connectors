// <copyright file="PrimaryLaborAccount.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PrimaryLaborAccount.
    /// </summary>
    public class PrimaryLaborAccount
    {
        /// <summary>
        /// Gets or sets the EffectiveDate.
        /// </summary>
        [XmlAttribute]
        public string EffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the ExpirationDate.
        /// </summary>
        [XmlAttribute]
        public string ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationPath.
        /// </summary>
        [XmlAttribute]
        public string OrganizationPath { get; set; }

        /// <summary>
        /// Gets or sets the LaborAccountName.
        /// </summary>
        [XmlAttribute]
        public string LaborAccountName { get; set; }
    }
}