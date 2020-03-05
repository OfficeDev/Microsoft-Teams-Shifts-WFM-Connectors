// <copyright file="Comments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Comments.
    /// </summary>
    [XmlRoot(ElementName = "Comments")]
    public class Comments
    {
        /// <summary>
        /// Gets or sets the list of <see cref="Comment"/>.
        /// </summary>
        [XmlElement(ElementName = "Comment")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<Comment> Comment { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}