// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PersonIdentity.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or sets the Person Number.
        /// </summary>
        [XmlAttribute(AttributeName = "PersonNumber")]
        public string PersonNumber { get; set; }
    }
}