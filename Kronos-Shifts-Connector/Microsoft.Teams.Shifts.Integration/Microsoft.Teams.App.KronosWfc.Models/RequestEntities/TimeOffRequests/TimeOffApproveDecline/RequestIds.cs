// <copyright file="RequestIds.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.TimeOffApproveDecline
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestIds.
    /// </summary>
    public class RequestIds
    {
        /// <summary>
        /// Gets or sets the RequestId.
        /// </summary>
        [XmlElement("RequestId")]
#pragma warning disable CA1819 // Properties should not return arrays
        public RequestId[] RequestId { get; set; }

#pragma warning restore CA1819 // Properties should not return arrays
    }
}