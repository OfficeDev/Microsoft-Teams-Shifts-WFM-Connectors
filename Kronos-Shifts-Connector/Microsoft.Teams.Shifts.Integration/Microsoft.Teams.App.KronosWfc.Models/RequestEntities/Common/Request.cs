// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// The class models Requests.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}
