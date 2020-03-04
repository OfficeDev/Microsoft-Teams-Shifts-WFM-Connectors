// <copyright file="Note.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models a Note.
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
        /// Gets or sets the timestamp.
        /// </summary>
        [XmlAttribute(AttributeName = "Timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [XmlAttribute(AttributeName = "Text")]
        public string Text { get; set; }
    }
}