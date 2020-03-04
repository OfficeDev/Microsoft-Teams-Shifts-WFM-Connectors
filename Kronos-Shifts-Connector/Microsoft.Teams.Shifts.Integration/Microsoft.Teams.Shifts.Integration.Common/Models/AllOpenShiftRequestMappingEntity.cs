// <copyright file="AllOpenShiftRequestMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the new schema for the Open Shift Request.
    /// </summary>
    public class AllOpenShiftRequestMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the OpenShiftId from Teams.
        /// </summary>
        [JsonProperty("TeamsOpenShiftId")]
        public string TeamsOpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the KronosOpenShiftUniqueId which is the hash.
        /// </summary>
        [JsonProperty("KronosOpenShiftUniqueId")]
        public string KronosOpenShiftUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the User AAD Object Id.
        /// </summary>
        [JsonProperty("AadUserId")]
        public string AadUserId { get; set; }

        /// <summary>
        /// Gets or sets the Kronos Person Number.
        /// </summary>
        [JsonProperty("KronosPersonNumber")]
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the Kronos Open Shift Request Id.
        /// </summary>
        [JsonProperty("KronosOpenShiftRequestId")]
        public string KronosOpenShiftRequestId { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonProperty("KronosStatus")]
        public string KronosStatus { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsStatus.
        /// </summary>
        [JsonProperty("ShiftsStatus")]
        public string ShiftsStatus { get; set; }
    }
}