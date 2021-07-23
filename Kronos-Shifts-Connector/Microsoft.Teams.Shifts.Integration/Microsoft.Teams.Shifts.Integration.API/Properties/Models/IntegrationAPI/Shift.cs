// <copyright file="Shift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the incoming Shift.
    /// </summary>
    public class Shift
    {
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
        /// Gets or sets the userId.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the schedulingGroupId.
        /// </summary>
        [JsonProperty("schedulingGroupId")]
        public string SchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets the eTag.
        /// </summary>
        [JsonProperty("eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the createdDateTime.
        /// </summary>
        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the lastModifiedDateTime.
        /// </summary>
        [JsonProperty("lastModifiedDateTime")]
        public DateTime LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the lastModifiedBy.
        /// </summary>
        [JsonProperty("lastModifiedBy")]
        public LastModifiedBy LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}