// <copyright file="Comment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Comment.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Gets or sets the CommentText.
        /// </summary>
        [XmlAttribute]
        public string CommentText { get; set; }

        /// <summary>
        /// Gets or sets the Notes.
        /// </summary>
        [XmlElement("Notes")]
        public Notes Notes { get; set; }
    }
}