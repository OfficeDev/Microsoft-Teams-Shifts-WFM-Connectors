// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the user.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the id of the user.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the user.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}