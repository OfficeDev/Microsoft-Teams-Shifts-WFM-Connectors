// <copyright file="RequestStatusChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.UpdateStatus
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the RequestStatusChange.
    /// </summary>
    public class RequestStatusChange
    {
        /// <summary>
        /// Gets or sets Swap Shift Request Id.
        /// </summary>
        [XmlAttribute]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets Swap Shift status name.
        /// </summary>
        [XmlAttribute]
        public string ToStatusName { get; set; }

        /// <summary>
        /// Gets or sets Swap Shift Request comments.
        /// </summary>
        [XmlElement("Comments")]
        public Comments Comments { get; set; }
    }
}