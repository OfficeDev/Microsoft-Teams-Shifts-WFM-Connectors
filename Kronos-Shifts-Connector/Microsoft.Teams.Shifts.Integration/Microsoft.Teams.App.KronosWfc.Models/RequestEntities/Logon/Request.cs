// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Logon
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create a login request to Kronos.
    /// </summary>
    [Serializable]
    public class Request
    {
        /// <summary>
        /// Gets or sets Object property in Kronos.
        /// </summary>
        [XmlAttribute]
#pragma warning disable CA1720 // Identifier contains type name
        public string Object { get; set; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Gets or sets Action property in Kronos.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets Username property in Kronos.
        /// </summary>
        [XmlAttribute]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets Password property in Kronos.
        /// </summary>
        [XmlAttribute]
        public string Password { get; set; }
    }
}