// <copyright file="GraphOpenShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShifts
{
    using System;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Open Shift response - meaning the 201 response.
    /// </summary>
    public class GraphOpenShift
    {
        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the etag.
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
        /// Gets or sets the lastModifiedBy.
        /// </summary>
        [JsonProperty("lastModifiedBy")]
        public object LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the draftOpenShift.
        /// </summary>
        [JsonProperty("draftOpenShift")]
        public object DraftOpenShift { get; set; }

        /// <summary>
        /// Gets or sets the sharedOpenShift.
        /// </summary>
        [JsonProperty("sharedOpenShift")]
        public OpenShiftItem SharedOpenShift { get; set; }
    }
}