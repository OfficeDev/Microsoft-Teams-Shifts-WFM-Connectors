// <copyright file="RequestStatusChanges.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.UpdateStatus
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestStatusChanges.
    /// </summary>
    public class RequestStatusChanges
    {
        /// <summary>
        /// Gets or sets Swap Shift Request status change.
        /// </summary>
        [XmlElement("RequestStatusChange")]
        public RequestStatusChange[] RequestStatusChange { get; set; }
    }
}