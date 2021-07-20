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

        /// <summary>
        /// Creates an <see cref="Employees"/> object with a <see cref="List{T}"/> of <see cref="PersonIdentity.PersonIdentity"/>objects.
        /// </summary>
        /// <param name="kronosIds">The kronos ids of the users.</param>
        /// <returns>A <see cref="Employees"/> object with a populated <see cref="List{T}"/> of <see cref="PersonIdentity.PersonIdentity"/> objects.</returns>
        public Employees Create(params string[] kronosIds)
        {
            this.PersonIdentity = new List<PersonIdentity>();
            foreach (var id in kronosIds)
            {
                this.PersonIdentity.Add(new PersonIdentity { PersonNumber = id });
            }

            return this;
        }
    }
}