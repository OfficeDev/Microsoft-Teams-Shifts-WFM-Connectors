// <copyright file="LastModifiedBy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the LastModifiedBy.
    /// </summary>
    public class LastModifiedBy
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }
    }
}