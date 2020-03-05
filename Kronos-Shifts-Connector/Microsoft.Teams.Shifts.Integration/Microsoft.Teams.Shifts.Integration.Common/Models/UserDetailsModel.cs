// <copyright file="UserDetailsModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    /// <summary>
    /// This class will model the necessary user information.
    /// </summary>
    public class UserDetailsModel
    {
        /// <summary>
        /// Gets or sets the Kronos Person Number.
        /// </summary>
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the Shift User Id.
        /// </summary>
        public string ShiftUserId { get; set; }

        /// <summary>
        /// Gets or sets the Shift User Id.
        /// </summary>
        public string ShiftTeamId { get; set; }

        /// <summary>
        /// Gets or sets the Shift scheduleGroupId.
        /// </summary>
        public string ShiftScheduleGroupId { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the Shift User Name.
        /// </summary>
        public string ShiftUserDisplayName { get; set; }
    }
}