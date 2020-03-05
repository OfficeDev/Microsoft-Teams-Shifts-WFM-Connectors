// <copyright file="Comments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Comments.
    /// </summary>
    public class Comments
    {
        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        [XmlElement("Comment")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Comment[] Comment { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}