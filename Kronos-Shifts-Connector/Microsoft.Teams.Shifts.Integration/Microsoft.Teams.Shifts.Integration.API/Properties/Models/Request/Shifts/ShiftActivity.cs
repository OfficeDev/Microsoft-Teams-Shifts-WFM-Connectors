// <copyright file="ShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Shift Activity.
    /// </summary>
    public class ShiftActivity
    {
        /// <summary>
        /// Gets or sets a value indicating whether gets or sets the isPaid.
        /// </summary>
        [JsonProperty("isPaid")]
        public bool IsPaid { get; set; }

        /// <summary>
        /// Gets or sets the startDateTime.
        /// </summary>
        [JsonProperty("startDateTime")]
        public DateTimeOffset StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the endDateTime.
        /// </summary>
        [JsonProperty("endDateTime")]
        public DateTimeOffset EndDateTime { get; set; }

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
    }
}