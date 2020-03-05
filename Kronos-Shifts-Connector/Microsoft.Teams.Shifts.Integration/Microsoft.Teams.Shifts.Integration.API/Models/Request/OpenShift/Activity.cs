// <copyright file="Activity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models an activity.
    /// </summary>
    public class Activity
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not an activity is paid.
        /// </summary>
        [JsonProperty("isPaid")]
        public bool IsPaid { get; set; }

        /// <summary>
        /// Gets or sets the StartDateTime.
        /// </summary>
        [JsonProperty("startDateTime")]
        public DateTimeOffset StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDateTime.
        /// </summary>
        [JsonProperty("endDateTime")]
        public DateTimeOffset EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the DisplayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}