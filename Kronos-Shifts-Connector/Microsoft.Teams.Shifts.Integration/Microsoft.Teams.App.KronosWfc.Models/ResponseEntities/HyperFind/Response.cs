// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the HyperFindQuery from Kronos.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the list of HyperFindResult.
        /// </summary>
        [XmlElement]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ResponseHyperFindResult> HyperFindResult { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets Error response.
        /// </summary>
        public Error Error { get; set; }
    }
}