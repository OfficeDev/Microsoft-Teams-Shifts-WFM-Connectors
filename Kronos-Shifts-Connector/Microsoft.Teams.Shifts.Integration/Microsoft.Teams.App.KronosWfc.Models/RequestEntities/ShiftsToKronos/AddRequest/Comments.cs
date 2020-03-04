// <copyright file="Comments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the comments.
    /// </summary>
    [XmlRoot(ElementName = "Comments")]
    public class Comments
    {
        /// <summary>
        /// Gets or sets the list of comments.
        /// </summary>
        [XmlElement(ElementName = "Comment")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<Comment> Comment { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}