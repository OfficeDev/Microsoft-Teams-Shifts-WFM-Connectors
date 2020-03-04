// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the employee.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement("PersonIdentity")]
        public PersonIdentity PersonIdentity { get; set; }
    }
}