// <copyright file="ShiftTeamDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the ShiftTeamDetails.
    /// </summary>
    public class ShiftTeamDetails
    {
        /// <summary>
        /// Gets or sets the list of Shift team with details.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftTeams> Value { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}