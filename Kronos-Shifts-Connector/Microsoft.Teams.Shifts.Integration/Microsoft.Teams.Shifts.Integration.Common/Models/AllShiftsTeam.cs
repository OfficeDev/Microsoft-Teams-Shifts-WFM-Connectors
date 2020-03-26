// <copyright file="AllShiftsTeam.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the ShiftTeams.
    /// </summary>
    public class AllShiftsTeam
    {
        /// <summary>
        /// Gets or sets the NextLink in Shifts for getting more timeoff requests.
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public Uri NextLink { get; set; }

        /// <summary>
        /// Gets or sets the Shifts teams.
        /// </summary>
        [JsonProperty("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftTeams> ShiftTeams { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
