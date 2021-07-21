// <copyright file="LastModifiedBy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the LastModifiedBy.
    /// </summary>
    public partial class LastModifiedBy
    {
        /// <summary>
        /// Gets or sets the Application.
        /// </summary>
        [JsonProperty("application")]
        public object Application { get; set; }

        /// <summary>
        /// Gets or sets the Device.
        /// </summary>
        [JsonProperty("device")]
        public object Device { get; set; }

        /// <summary>
        /// Gets or sets the Conversation.
        /// </summary>
        [JsonProperty("conversation")]
        public object Conversation { get; set; }

        /// <summary>
        /// Gets or sets the User.
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }
    }
}