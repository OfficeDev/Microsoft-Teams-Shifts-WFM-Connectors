// <copyright file="Comment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the comment.
    /// </summary>
    [XmlRoot(ElementName = "Comment")]
    public class Comment
    {
        /// <summary>
        /// Gets or sets the Notes.
        /// </summary>
        [XmlElement(ElementName = "Notes")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<Notes> Notes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the comment text.
        /// </summary>
        [XmlAttribute(AttributeName = "CommentText")]
        public string CommentText { get; set; }
    }
}