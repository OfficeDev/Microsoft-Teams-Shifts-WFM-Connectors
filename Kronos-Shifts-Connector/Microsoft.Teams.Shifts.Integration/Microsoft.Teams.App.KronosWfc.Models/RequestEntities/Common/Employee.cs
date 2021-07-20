// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
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

        /// <summary>
        /// Creates a list of employees.
        /// </summary>
        /// <param name="id">The kronos id of the user.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="Employee"/> objects.</returns>
        public List<Employee> Create(string id)
        {
            this.PersonIdentity = new PersonIdentity { PersonNumber = id };

            return new List<Employee> { this };
        }
    }
}
