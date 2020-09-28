// <copyright file="TeamToDepartmentJobMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the Team-to-DepartmentJob mapping.
    /// </summary>
    public class TeamToDepartmentJobMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the Kronos Time Zone.
        /// </summary>
        public string KronosTimeZone { get; set; }

        /// <summary>
        /// Gets or sets the Team name in Shifts.
        /// </summary>
        public string ShiftsTeamName { get; set; }

        /// <summary>
        /// Gets or sets the Team Id.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Team scheduled group id.
        /// </summary>
        public string TeamsScheduleGroupId { get; set; }

        /// <summary>
        /// Gets or sets the Team scheduled group name.
        /// </summary>
        public string TeamsScheduleGroupName { get; set; }
    }
}