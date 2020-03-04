// <copyright file="Identity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Identity.
    /// </summary>
    public class Identity
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement("PersonIdentity")]
        public PersonIdentity PersonIdentit { get; set; }
    }
}