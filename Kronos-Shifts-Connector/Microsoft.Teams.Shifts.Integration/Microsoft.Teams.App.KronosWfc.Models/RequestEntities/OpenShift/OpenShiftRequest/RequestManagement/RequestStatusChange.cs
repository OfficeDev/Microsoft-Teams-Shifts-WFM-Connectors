// <copyright file="RequestStatusChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the RequestStatusChange.
    /// </summary>
    public class RequestStatusChange
    {
        /// <summary>
        /// Gets or sets the RequestId.
        /// </summary>
        [XmlAttribute]
        public string RequestId { get; set; }

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
    }
}