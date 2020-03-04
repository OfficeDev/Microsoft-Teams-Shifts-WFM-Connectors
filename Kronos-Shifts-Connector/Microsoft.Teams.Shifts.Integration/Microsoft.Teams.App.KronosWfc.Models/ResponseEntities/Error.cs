// <copyright file="Error.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to serialize all errors from Kronos.
    /// </summary>
    [Serializable]
#pragma warning disable CA1716 // Identifiers should not match keywords
    public class Error
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        /// <summary>
        /// Gets or sets Error message.
        /// </summary>
        [XmlAttribute]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets Error code.
        /// </summary>
        [XmlAttribute]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets AtIndex.
        /// </summary>
        [XmlAttribute]
        public string AtIndex { get; set; }

        /// <summary>
        /// Gets or sets detailed errors.
        /// </summary>
        [XmlElement("DetailErrors")]
#pragma warning disable CA2235 // Mark all non-serializable fields
        public ErrorArr DetailErrors { get; set; }
#pragma warning restore CA2235 // Mark all non-serializable fields
    }
}