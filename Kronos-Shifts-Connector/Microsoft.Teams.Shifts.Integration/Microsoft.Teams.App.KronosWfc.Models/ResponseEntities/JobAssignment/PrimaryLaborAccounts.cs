// <copyright file="PrimaryLaborAccounts.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PrimaryLaborAccounts.
    /// </summary>
    public class PrimaryLaborAccounts
    {
        /// <summary>
        /// Gets or sets the PrimaryLaborAccount.
        /// </summary>
        [XmlElement("PrimaryLaborAccount")]
        public PrimaryLaborAccount PrimaryLaborAcc { get; set; }
    }
}