// <copyright file="IncomingRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShift
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class is used to create request for Open shift in Kronos.
    /// </summary>
    public class IncomingRequest
    {
        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [JsonProperty("url")]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        [JsonProperty("body")]
        public Body Body { get; set; }
    }
}