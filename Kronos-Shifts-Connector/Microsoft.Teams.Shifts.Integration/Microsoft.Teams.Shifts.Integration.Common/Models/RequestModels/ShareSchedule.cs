// <copyright file="ShareSchedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Share Schedule Request Entity.
    /// </summary>
    public class ShareSchedule
    {
        /// <summary>
        /// Gets or sets a value indicating whether to notify the team.
        /// </summary>
        [JsonProperty(PropertyName = "notifyTeam")]
        public bool NotifyTeam { get; set; }

        /// <summary>
        /// Gets or sets the start date time.
        /// </summary>
        [JsonProperty(PropertyName = "startDateTime")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date time.
        /// </summary>
        [JsonProperty(PropertyName = "endDateTime")]
        public DateTime EndDateTime { get; set; }
    }
}