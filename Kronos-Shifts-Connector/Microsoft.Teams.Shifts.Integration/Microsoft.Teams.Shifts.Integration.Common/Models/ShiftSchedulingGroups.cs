// <copyright file="ShiftSchedulingGroups.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the ShiftSchedulingGroups.
    /// </summary>
    public class ShiftSchedulingGroups
    {
        /// <summary>
        /// Gets or sets the Scheduling group Id.
        /// </summary>
        [JsonProperty("Id")]
        public string ShiftSchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets Scheduling group Name.
        /// </summary>
        [JsonProperty("DisplayName")]
        public string ShiftSchedulingGroupName { get; set; }
    }
}