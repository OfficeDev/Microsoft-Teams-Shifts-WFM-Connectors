// <copyright file="Shift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using Newtonsoft.Json;

    /// <summary>
    /// Gets or sets the actual shift.
    /// </summary>
    public class Shift
    {
        // [JsonProperty("id")]
        // public string Id { get; set; }

        /// <summary>
        /// Gets or sets the userId.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the userId.
        /// </summary>
        [JsonProperty("schedulingGroupId")]
        public string SchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets the sharedShift.
        /// </summary>
        [JsonProperty("sharedShift")]
        public SharedShift SharedShift { get; set; }

        /// <summary>
        /// Gets or sets the draftShift.
        /// </summary>
        [JsonProperty("draftShift")]
        public DraftShift DraftShift { get; set; }

        /// <summary>
        /// Gets or sets the hash of Kronos start and end date time.
        /// </summary>
        public string KronosUniqueId { get; set; }
    }
}