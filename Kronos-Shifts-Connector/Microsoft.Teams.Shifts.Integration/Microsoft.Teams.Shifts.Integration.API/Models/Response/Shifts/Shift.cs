// <copyright file="Shift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.Shifts
{
    using System;
    using Microsoft.Teams.Shifts.Integration.API.Models.Request;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Shift response.
    /// </summary>
    public class Shift
    {
        /// <summary>
        /// Gets or sets the @odata.context.
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the @odata.etag.
        /// </summary>
        [JsonProperty("@odata.etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

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
        /// Gets or sets the schedulingGroupId.
        /// </summary>
        [JsonProperty("schedulingGroupId")]
        public string SchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets the userId.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the draftShift.
        /// </summary>
        [JsonProperty("draftShift")]
        public object DraftShift { get; set; }

        /// <summary>
        /// Gets or sets the lastModifiedBy.
        /// </summary>
        [JsonProperty("lastModifiedBy")]
        public LastModifiedBy LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the sharedShift.
        /// </summary>
        [JsonProperty("sharedShift")]
        public SharedShift SharedShift { get; set; }
    }
}