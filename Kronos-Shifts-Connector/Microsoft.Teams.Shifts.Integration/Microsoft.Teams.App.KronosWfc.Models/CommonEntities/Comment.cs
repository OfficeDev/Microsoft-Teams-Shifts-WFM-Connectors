// <copyright file="Comment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.CommonEntities
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Comment.
    /// </summary>
    [XmlRoot(ElementName = "Comment")]
    public class Comment
    {
        /// <summary>
        /// Gets or sets the Notes.
        /// </summary>
        [XmlElement(ElementName = "Notes")]
        public List<Notes> Notes { get; set; }

        /// <summary>
        /// Gets or sets the CommentText.
        /// </summary>
        [XmlAttribute(AttributeName = "CommentText")]
        public string CommentText { get; set; }
    }
}