// <copyright file="OpenShiftIS.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Open Shift.
    /// </summary>
    public class OpenShiftIS
    {
        /// <summary>
        /// Gets or sets the sharedOpenShift.
        /// </summary>
        [JsonProperty("sharedOpenShift")]
        public SharedOpenShift SharedOpenShift { get; set; }

        /// <summary>
        /// Gets or sets the draftOpenShift.
        /// </summary>
        [JsonProperty("draftOpenShift")]
        public DraftOpenShift DraftOpenShift { get; set; }

        /// <summary>
        /// Gets or sets the schedulingGroupId.
        /// </summary>
        [JsonProperty("schedulingGroupId")]
        public string SchedulingGroupId { get; set; }

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