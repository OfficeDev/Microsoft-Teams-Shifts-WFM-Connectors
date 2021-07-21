// <copyright file="Activity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the activity.
    /// </summary>
    public class Activity
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not an activity is paid.
        /// </summary>
        [JsonProperty("isPaid")]
        public bool IsPaid { get; set; }

        /// <summary>
        /// Gets or sets the startDateTime.
        /// </summary>
        [JsonProperty("startDateTime")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the endDateTime.
        /// </summary>
        [JsonProperty("endDateTime")]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        [JsonProperty("theme")]
        public string Theme { get; set; }
    }
}