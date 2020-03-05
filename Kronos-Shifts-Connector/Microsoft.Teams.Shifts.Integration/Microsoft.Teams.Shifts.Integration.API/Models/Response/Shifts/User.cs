// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.Shifts
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the user.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the id (the AAD Object ID).
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}