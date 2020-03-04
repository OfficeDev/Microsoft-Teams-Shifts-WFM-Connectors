// <copyright file="Note.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Note.
    /// </summary>
    [XmlRoot(ElementName = "Note")]
    public class Note
    {
        /// <summary>
        /// Gets or sets the DataSource.
        /// </summary>
        [XmlElement(ElementName = "DataSource")]
        public DataSource DataSource { get; set; }

        /// <summary>
        /// Gets or sets the Timestamp.
        /// </summary>
        [XmlAttribute(AttributeName = "Timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the Text.
        /// </summary>
        [XmlAttribute(AttributeName = "Text")]
        public string Text { get; set; }
    }
}