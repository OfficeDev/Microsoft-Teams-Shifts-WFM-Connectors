// <copyright file="TimeOffReason.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the TimeOffReason.
    /// </summary>
    public class TimeOffReason
    {
        /// <summary>
        /// Gets or sets the DisplayName.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "displayName", Required = Required.Default)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the IconType.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "iconType", Required = Required.Default)]
        public string IconType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not a mapping is active.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isActive", Required = Required.Default)]
        public bool IsActive { get; set; }
    }
}