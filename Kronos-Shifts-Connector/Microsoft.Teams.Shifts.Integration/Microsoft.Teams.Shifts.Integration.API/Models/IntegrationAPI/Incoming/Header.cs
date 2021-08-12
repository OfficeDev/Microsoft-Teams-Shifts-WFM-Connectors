// <copyright file="Header.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the header of the incoming request.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Gets or sets the X-MS-Transaction-ID.
        /// </summary>
        [JsonProperty("X-MS-Transaction-ID")]
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the X-MS-Expires property.
        /// </summary>
        [JsonProperty("X-MS-Expires")]
        public DateTime Expiration { get; set; }
    }
}