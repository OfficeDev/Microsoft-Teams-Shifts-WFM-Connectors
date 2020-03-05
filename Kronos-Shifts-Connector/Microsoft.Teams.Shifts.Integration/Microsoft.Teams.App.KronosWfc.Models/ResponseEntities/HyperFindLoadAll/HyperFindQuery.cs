// <copyright file="HyperFindQuery.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFindLoadAll
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the HyperFindQuery from Kronos.
    /// </summary>
    public class HyperFindQuery
    {
        /// <summary>
        /// Gets or sets the Description.
        /// </summary>
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets HyperFindQueryName.
        /// </summary>
        [XmlAttribute(AttributeName = "HyperFindQueryName")]
        public string HyperFindQueryName { get; set; }

        /// <summary>
        /// Gets or sets VisibilityCode.
        /// </summary>
        [XmlAttribute(AttributeName = "VisibilityCode")]
        public string VisibilityCode { get; set; }
    }
}