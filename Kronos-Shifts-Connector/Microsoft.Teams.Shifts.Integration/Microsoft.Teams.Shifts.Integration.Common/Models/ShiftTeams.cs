// <copyright file="ShiftTeams.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the ShiftTeams.
    /// </summary>
    public class ShiftTeams
    {
        /// <summary>
        /// Gets or sets the Team Id.
        /// </summary>
        [JsonProperty("Id")]
        public string ShiftTeamId { get; set; }

        /// <summary>
        /// Gets or sets Team Name.
        /// </summary>
        [JsonProperty("DisplayName")]
        public string ShiftTeamName { get; set; }

        /// <summary>
        /// Gets or sets the list of Shift scheduling groups.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftSchedulingGroups> SchedulingGroups { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}