// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Persons Identity.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or sets the Person number.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set; }
    }
}
