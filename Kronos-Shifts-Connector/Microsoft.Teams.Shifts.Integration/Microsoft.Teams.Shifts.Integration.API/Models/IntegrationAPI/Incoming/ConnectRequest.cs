// <copyright file="ConnectRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class defines the connect request model.
    /// </summary>
    public class ConnectRequest
    {
        /// <summary>
        /// Gets or sets the Tenant ID.
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the User ID.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }
}