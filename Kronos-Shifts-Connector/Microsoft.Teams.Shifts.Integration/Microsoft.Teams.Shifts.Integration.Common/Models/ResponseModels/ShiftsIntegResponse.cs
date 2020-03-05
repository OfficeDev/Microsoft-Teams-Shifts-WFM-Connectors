// <copyright file="ShiftsIntegResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models a single Integration API response.
    /// </summary>
    public class ShiftsIntegResponse
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [JsonProperty("body")]
        public Body Body { get; set; }
    }
}