// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
    public class RequestMgmt
    {
        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
        [XmlElement]
        public Employees Employees { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlAttribute]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the StatusName.
        /// </summary>
        [XmlAttribute]
        public string StatusName { get; set; }
    }
}