// <copyright file="Employee.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest.ApproveDecline
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employee.
    /// </summary>
    [XmlRoot(ElementName = "Employee")]
    public class Employee
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement(ElementName = "PersonIdentity")]
        public PersonIdentity PersonIdentity { get; set; }
    }
}