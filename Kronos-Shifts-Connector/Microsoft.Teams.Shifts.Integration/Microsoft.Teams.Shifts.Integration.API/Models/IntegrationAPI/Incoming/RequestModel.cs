// <copyright file="RequestModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class represents the RequestModel for the incoming requests from Shifts.
    /// </summary>
    public class RequestModel
    {
        /// <summary>
        /// Gets or sets the requests array.
        /// </summary>
        [JsonProperty("requests")]
#pragma warning disable CA1819 // Properties should not return arrays
        public IncomingRequest[] Requests { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}