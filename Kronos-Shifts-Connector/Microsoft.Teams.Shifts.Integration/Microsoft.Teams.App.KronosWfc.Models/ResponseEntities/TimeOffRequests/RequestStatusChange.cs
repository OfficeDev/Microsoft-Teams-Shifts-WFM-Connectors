// <copyright file="RequestStatusChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the RequestStatusChange.
    /// </summary>
    [XmlRoot(ElementName = "RequestStatusChange")]
    public class RequestStatusChange
    {
        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        [XmlElement(ElementName = "User")]
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the Comments.
        /// </summary>
        [XmlElement(ElementName = "Comments")]
        public List<Comment> Comments { get; set; }

        /// <summary>
        /// Gets or sets the ToStatusName.
        /// </summary>
        [XmlAttribute(AttributeName = "ToStatusName")]
        public string ToStatusName { get; set; }

        /// <summary>
        /// Gets or sets the ChangeDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "ChangeDateTime")]
        public string ChangeDateTime { get; set; }

        /// <summary>
        /// Gets or sets the FromStatusName.
        /// </summary>
        [XmlAttribute(AttributeName = "FromStatusName")]
        public string FromStatusName { get; set; }
    }
}