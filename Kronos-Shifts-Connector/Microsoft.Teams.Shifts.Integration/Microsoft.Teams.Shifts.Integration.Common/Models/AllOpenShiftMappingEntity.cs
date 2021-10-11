// <copyright file="AllOpenShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System;
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
        [JsonProperty("KronosOpenShiftUniqueId")]
        public string KronosOpenShiftUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the KronosSlots.
        /// </summary>
        [JsonProperty("KronosSlots")]
        public int KronosSlots { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [JsonProperty("OrgJobPath")]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the open shift start date.
        /// </summary>
        [JsonProperty("OpenShiftStartDate")]
        public DateTime OpenShiftStartDate { get; set; }
    }
}