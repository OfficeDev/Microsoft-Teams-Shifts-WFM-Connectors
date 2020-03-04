// <copyright file="RequestStatusChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest;

    /// <summary>
    /// This class models the RequestStatusChange.
    /// </summary>
    public class RequestStatusChange
    {
        /// <summary>
        /// Gets or sets the RequestId.
        /// </summary>
        [XmlAttribute]
        public string FromStatusName { get; set; }

        /// <summary>
        /// Gets or sets the ToStatusName.
        /// </summary>
        [XmlAttribute]
        public string ToStatusName { get; set; }

        /// <summary>
        /// Gets or sets the Comments.
        /// </summary>
        [XmlElement("Comments")]
        public Comment Comments { get; set; }

        /// <summary>
        /// Gets or sets the ChangeDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "ChangeDateTime")]
        public string ChangeDateTime { get; set; }
    }
}