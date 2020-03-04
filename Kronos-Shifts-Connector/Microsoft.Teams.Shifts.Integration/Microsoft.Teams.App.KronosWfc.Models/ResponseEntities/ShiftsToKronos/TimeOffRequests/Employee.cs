// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employee.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Gets or sets the Person Identity.
        /// </summary>
        [XmlElement("PersonIdentity")]
        public PersonIdentity PersonIdentity { get; set; }
    }
}
