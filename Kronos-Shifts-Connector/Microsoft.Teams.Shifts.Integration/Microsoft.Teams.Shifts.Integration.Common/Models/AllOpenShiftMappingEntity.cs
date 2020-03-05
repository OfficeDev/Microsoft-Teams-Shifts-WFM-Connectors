// <copyright file="AllOpenShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the new Open Shift schema.
    /// </summary>
    public class AllOpenShiftMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the TeamsOpenShiftId.
        /// </summary>
        [JsonProperty("TeamsOpenShiftId")]
        public string TeamsOpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the KronosSlots.
        /// </summary>
        [JsonProperty("KronosSlots")]
        public string KronosSlots { get; set; }

        /// <summary>
        /// Gets or sets the SchedulingGroupId.
        /// </summary>
        [JsonProperty("SchedulingGroupId")]
        public string SchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [JsonProperty("OrgJobPath")]
        public string OrgJobPath { get; set; }
    }
}