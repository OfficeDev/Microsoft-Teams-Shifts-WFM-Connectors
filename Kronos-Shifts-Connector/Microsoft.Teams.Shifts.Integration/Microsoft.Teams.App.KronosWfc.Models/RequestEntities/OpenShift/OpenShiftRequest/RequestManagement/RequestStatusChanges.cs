// <copyright file="RequestStatusChanges.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestStatusChanges.
    /// </summary>
    public class RequestStatusChanges
    {
        /// <summary>
        /// Gets or sets the RequestStatusChange.
        /// </summary>
        [XmlElement("RequestStatusChange")]
#pragma warning disable CA1819 // Properties should not return arrays
        public RequestStatusChange[] RequestStatusChange { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}