// <copyright file="Employees.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employees.
    /// </summary>
    public class Employees
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement]
        public List<PersonIdentity> PersonIdentity { get; set; }
    }
}