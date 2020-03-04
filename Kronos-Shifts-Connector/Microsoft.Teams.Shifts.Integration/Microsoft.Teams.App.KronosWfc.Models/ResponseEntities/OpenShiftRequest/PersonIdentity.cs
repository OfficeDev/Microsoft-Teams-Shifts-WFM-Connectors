// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PersonIdentity.
    /// </summary>
    [XmlRoot(ElementName = "PersonIdentity")]
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        [XmlAttribute(AttributeName = "PersonNumber")]
        public string PersonNumber { get; set; }
    }
}