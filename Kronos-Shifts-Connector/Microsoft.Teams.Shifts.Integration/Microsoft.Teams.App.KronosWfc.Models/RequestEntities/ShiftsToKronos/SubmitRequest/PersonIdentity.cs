// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Person Identity.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or sets person numbers.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set; }
    }
}
