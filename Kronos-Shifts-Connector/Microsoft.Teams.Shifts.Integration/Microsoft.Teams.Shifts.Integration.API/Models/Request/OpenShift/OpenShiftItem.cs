// <copyright file="OpenShiftItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the OpenShiftItem.
    /// </summary>
    public class OpenShiftItem
    {
        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets the openSlotCount.
        /// </summary>
        [JsonProperty("openSlotCount")]
        public int OpenSlotCount { get; set; }

        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

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
        public List<Activity> Activities { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only
    }
}