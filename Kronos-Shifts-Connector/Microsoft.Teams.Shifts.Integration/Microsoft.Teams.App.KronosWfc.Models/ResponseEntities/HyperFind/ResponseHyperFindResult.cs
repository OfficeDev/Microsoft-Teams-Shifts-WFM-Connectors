// <copyright file="ResponseHyperFindResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the HyperFindQuery from Kronos.
    /// </summary>
    public class ResponseHyperFindResult
    {
        /// <summary>
        /// Gets or sets the FullName.
        /// </summary>
        [XmlAttribute]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set; }
    }
}