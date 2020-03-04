// <copyright file="PayCodeToTimeOffReasonsMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the PayCodeToTimeOffReason Mapping.
    /// </summary>
    public class PayCodeToTimeOffReasonsMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the KronosPayCode.
        /// </summary>
        [Key]
        [JsonProperty("KronosPayCode")]
        public string KronosPayCode { get; set; }

        /// <summary>
        /// Gets or sets the TimeOffReasonId.
        /// </summary>
        [Key]
        [JsonProperty("TimeOffReasonId")]
        public string TimeOffReasonId { get; set; }

        /// <summary>
        /// Gets or sets the TeamsId.
        /// </summary>
        public string TeamsId { get; set; }
    }
}