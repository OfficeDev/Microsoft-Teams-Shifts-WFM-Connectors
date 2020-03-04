// <copyright file="RequestHyperFindQuery.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.HyperFind
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create hyperfind query requests.
    /// </summary>
    public class RequestHyperFindQuery
    {
        /// <summary>
        /// Gets or sets the HyperFindQueryName.
        /// </summary>
        [XmlAttribute]
        public string HyperFindQueryName { get; set; }

        /// <summary>
        /// Gets or sets the VisibilityCode.
        /// </summary>
        [XmlAttribute]
        public string VisibilityCode { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }
    }
}