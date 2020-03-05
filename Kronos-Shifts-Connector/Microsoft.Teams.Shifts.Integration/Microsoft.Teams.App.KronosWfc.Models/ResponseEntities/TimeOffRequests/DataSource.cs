// <copyright file="DataSource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the DataSource.
    /// </summary>
    [XmlRoot(ElementName = "DataSource")]
    public class DataSource
    {
        /// <summary>
        /// Gets or sets the ClientName.
        /// </summary>
        [XmlAttribute(AttributeName = "ClientName")]
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the FunctionalAreaCode.
        /// </summary>
        [XmlAttribute(AttributeName = "FunctionalAreaCode")]
        public string FunctionalAreaCode { get; set; }

        /// <summary>
        /// Gets or sets the ServerName.
        /// </summary>
        [XmlAttribute(AttributeName = "ServerName")]
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the UserName.
        /// </summary>
        [XmlAttribute(AttributeName = "UserName")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the FunctionalAreaName.
        /// </summary>
        [XmlAttribute(AttributeName = "FunctionalAreaName")]
        public string FunctionalAreaName { get; set; }

        /// <summary>
        /// Gets or sets the DataSource.
        /// </summary>
        [XmlElement(ElementName = "DataSource")]
        public DataSource DataSrc { get; set; }
    }
}