// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the User.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
