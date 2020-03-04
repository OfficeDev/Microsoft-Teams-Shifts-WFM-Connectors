// <copyright file="Employees.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest.ApproveDecline
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employees.
    /// </summary>
    [XmlRoot(ElementName = "Employees")]
    public class Employees
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement(ElementName = "PersonIdentity")]
        public PersonIdentity PersonIdentity { get; set; }
    }
}