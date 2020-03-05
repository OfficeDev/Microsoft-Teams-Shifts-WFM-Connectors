// <copyright file="SharedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Shared Shift.
    /// </summary>
    public class SharedShift
    {
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
        /// Gets or sets the theme.
        /// </summary>
        [JsonProperty("theme")]
        public string Theme { get; set; }

        /// <summary>
        /// Gets or sets the activities.
        /// </summary>
        [JsonProperty("activities")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftActivity> Activities { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}