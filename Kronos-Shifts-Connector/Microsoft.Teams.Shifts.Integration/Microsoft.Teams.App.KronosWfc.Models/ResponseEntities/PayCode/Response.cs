// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.PayCodes
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Response class for paycode fetch from Kronos.
    /// </summary>
    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        /// <summary>
        /// Gets or sets Kronos paycode name.
        /// </summary>
        [XmlElement(ElementName = "PayCode")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PayCode> PayCode { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets Kronos status of Paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets action to be taken on paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets excuce flag of Kronos.
        /// </summary>
        [XmlAttribute(AttributeName = "ExcuseAbsenceFlag")]
        public string ExcuseAbsenceFlag { get; set; }
    }
}
