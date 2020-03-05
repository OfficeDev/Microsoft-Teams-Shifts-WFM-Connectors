// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFindLoadAll
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the HyperFindQuery from Kronos.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets HyperFindQuery.
        /// </summary>
        [XmlElement(ElementName = "HyperFindQuery")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<HyperFindQuery> HyperFindQuery { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets Error response.
        /// </summary>
        public Error Error { get; set; }
    }
}