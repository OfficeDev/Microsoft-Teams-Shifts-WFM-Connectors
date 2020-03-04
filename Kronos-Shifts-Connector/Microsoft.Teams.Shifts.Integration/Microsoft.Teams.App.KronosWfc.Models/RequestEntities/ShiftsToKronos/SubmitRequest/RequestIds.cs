// <copyright file="RequestIds.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Submit request Ids.
    /// </summary>
    public class RequestIds
    {
        /// <summary>
        /// Gets or sets submit request Ids.
        /// </summary>
        [XmlElement("RequestId")]
#pragma warning disable CA1819 // Properties should not return arrays
        public RequestId[] RequestId { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}