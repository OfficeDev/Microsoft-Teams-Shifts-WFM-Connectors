// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employee details.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Gets or sets person Identity of an employee.
        /// </summary>
        [XmlElement("PersonIdentity")]
        public PersonIdentity PersonIdentity { get; set; }
    }
}
