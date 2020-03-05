// <copyright file="Note.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Note.
    /// </summary>
    public class Note
    {
        /// <summary>
        /// Gets or sets the Text.
        /// </summary>
        [XmlAttribute]
        public string Text { get; set; }
    }
}