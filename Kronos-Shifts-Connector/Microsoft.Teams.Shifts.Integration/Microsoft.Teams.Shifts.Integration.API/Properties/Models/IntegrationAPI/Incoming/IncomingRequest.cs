// <copyright file="IncomingRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This class models the request.
    /// </summary>
    public class IncomingRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        [JsonProperty("url")]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets or sets the headers of the request.
        /// </summary>
        [JsonProperty("headers")]
        public Header Headers { get; set; }

        /// <summary>
        /// Gets or sets the body of the incoming request.
        /// </summary>
        [JsonProperty("body")]
#pragma warning disable CA2227 // Collection properties should be read only
        public JObject Body { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}