// <copyright file="DraftOpenShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the DraftOpenShift.
    /// </summary>
    public class DraftOpenShift
    {
        /// <summary>
        /// Gets or sets the open slot count.
        /// </summary>
        [JsonProperty("openSlotCount")]
        public int OpenSlotCount { get; set; }

        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets the activities.
        /// </summary>
        [JsonProperty("activities")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<Activity> Activities { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets a value indicating whether or not an open shift is active.
        /// </summary>
        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

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
        /// Gets or sets the theme.
        /// </summary>
        [JsonProperty("theme")]
        public string Theme { get; set; }
    }
}