// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.HyperFind
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create top level placeholder to wrap actual hyperfind query requests.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the HyperFindQuery request.
        /// </summary>
        public RequestHyperFindQuery HyperFindQuery { get; set; }

        /// <summary>
        /// Gets or sets the Action for the request.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}