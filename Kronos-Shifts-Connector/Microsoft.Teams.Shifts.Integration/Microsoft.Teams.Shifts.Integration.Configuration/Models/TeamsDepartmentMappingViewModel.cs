// <copyright file="TeamsDepartmentMappingViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    /// <summary>
    /// Defines the view model of the mapping between an organisation job path in Kronos and a scheduling group within a team shifts schedule.
    /// </summary>
    public class TeamsDepartmentMappingViewModel
    {
        /// <summary>
        /// Gets/Sets the workforce integration id associated with the kronos organisation job path to teams mapping.
        /// </summary>
        public string WorkforceIntegrationId { get; internal set; }

        /// <summary>
        /// Gets/Sets the Kronos organisation job path being mapped.
        /// </summary>
        public string KronosOrgJobPath { get; internal set; }

        /// <summary>
        /// Gets/Sets the time zone being used by the organisation job path.
        /// </summary>
        public string KronosTimeZone { get; internal set; }

        /// <summary>
        /// Gets/Sets the name of the team.
        /// </summary>
        public string ShiftsTeamName { get; internal set; }

        /// <summary>
        /// Gets/Sets the ID of the team the organisation job path is mapped to.
        /// </summary>
        public string TeamId { get; internal set; }

        /// <summary>
        /// Gets/Sets the ID of the scheduling group the organisation job path is mapped to.
        /// </summary>
        public string TeamsScheduleGroupId { get; internal set; }

        /// <summary>
        /// Gets/Sets the name of the scheduling group the organisation job path is mapped to.
        /// </summary>
        public string TeamsScheduleGroupName { get; internal set; }
    }
}
