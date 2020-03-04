// <copyright file="TeamsDepartmentMappingModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Represents the Team to department entity used to bind data to datatable in Team department mapping.
    /// </summary>
    public class TeamsDepartmentMappingModel : TableEntity
    {
        /// <summary>
        /// Gets or sets the Shift team name.
        /// </summary>
        public string ShiftsTeamName { get; set; }

        /// <summary>
        /// Gets or sets the Shift team Id.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Shift scheduling group Id.
        /// </summary>
        public string TeamsScheduleGroupId { get; set; }

        /// <summary>
        /// Gets or sets the Shift scheduling group name.
        /// </summary>
        public string TeamsScheduleGroupName { get; set; }
    }
}