// <copyright file="RequestStatusChanges.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestStatusChanges.
    /// </summary>
    [XmlRoot(ElementName = "RequestStatusChanges")]
    public class RequestStatusChanges
    {
        /// <summary>
        /// Gets or sets the RequestStatusChange.
        /// </summary>
        [XmlElement(ElementName = "RequestStatusChange")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<RequestStatusChange> RequestStatusChange { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}