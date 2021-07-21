// <copyright file="LastModifiedBy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.Shifts
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class details the last modified by attributes.
    /// </summary>
    public class LastModifiedBy
    {
        /// <summary>
        /// Gets or sets the application.
        /// </summary>
        [JsonProperty("application")]
        public object Application { get; set; }

        /// <summary>
        /// Gets or sets the device.
        /// </summary>
        [JsonProperty("device")]
        public object Device { get; set; }

        /// <summary>
        /// Gets or sets the conversation.
        /// </summary>
        [JsonProperty("conversation")]
        public object Conversation { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }
    }
}