// <copyright file="Notes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.CommonEntities
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Notes.
    /// </summary>
    [XmlRoot(ElementName = "Notes")]
    public class Notes
    {
        /// <summary>
        /// Gets or sets a note.
        /// </summary>
        [XmlElement(ElementName = "Note")]
        public List<Note> Note { get; set; }
    }
}