// <copyright file="Notes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Notes.
    /// </summary>
    public class Notes
    {
        /// <summary>
        /// Gets or sets a Note.
        /// </summary>
        [XmlElement("Note")]
        public Note Note { get; set; }
    }
}