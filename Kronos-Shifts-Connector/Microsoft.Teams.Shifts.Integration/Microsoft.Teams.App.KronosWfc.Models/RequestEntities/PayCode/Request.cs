// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.PayCodes
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PayCode Request.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class Request
    {
        /// <summary>
        /// Gets or sets the Pay code of Time Off.
        /// </summary>
        [XmlElement(ElementName = "PayCode")]
        public string PayCode { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}