// <copyright file="ShiftModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    /// <summary>
    /// This class will model the necessary Shift's team information.
    /// </summary>
    public class ShiftModel
    {
        /// <summary>
        /// Gets or sets the TeamID for the team that has been mapped.
        /// </summary>
        public string ShiftsTeamId { get; set; }

        /// <summary>
        /// Gets or sets the TeamName for the team that has been mapped.
        /// </summary>
        public string ShiftsTeamName { get; set; }
    }
}