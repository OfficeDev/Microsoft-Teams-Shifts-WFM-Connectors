﻿// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// The Employee tag.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Gets or sets the Person Identity element.
        /// </summary>
        [XmlElement]
        public PersonIdentity PersonIdentity { get; set; }
    }
}
