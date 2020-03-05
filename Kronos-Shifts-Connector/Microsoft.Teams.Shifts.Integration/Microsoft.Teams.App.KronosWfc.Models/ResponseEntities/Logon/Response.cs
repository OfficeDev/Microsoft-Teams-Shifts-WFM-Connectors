// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create top level placeholder to wrap the response received from Kronos.
    /// </summary>
    [Serializable]
    public class Response
    {
        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Timeout.
        /// </summary>
        [XmlAttribute]
        public string Timeout { get; set; }

        /// <summary>
        /// Gets or sets the Message.
        /// </summary>
        [XmlAttribute]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the ErrorCode.
        /// </summary>
        [XmlAttribute]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the Username.
        /// </summary>
        [XmlAttribute]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the Object.
        /// </summary>
        [XmlAttribute]
#pragma warning disable CA1720 // Identifier contains type name
        public string Object { get; set; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the Session Id.
        /// </summary>
        [XmlIgnore]
        public string Jsession { get; set; }
    }
}