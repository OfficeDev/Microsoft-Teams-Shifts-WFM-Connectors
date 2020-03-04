// <copyright file="UserMappingViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.Rendering;

    /// <summary>
    /// This class will model the necessary configuration information.
    /// </summary>
    public class UserMappingViewModel
    {
        /// <summary>
        /// Gets or sets the ShiftsTeamId.
        /// </summary>
        public string ShiftsTeamId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsUserId.
        /// </summary>
        public string ShiftsUserId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsUserIName.
        /// </summary>
        public string ShiftsUserName { get; set; }

        /// <summary>
        /// Gets or sets the KronosUserId.
        /// </summary>
        public string KronosUserId { get; set; }

        /// <summary>
        /// Gets or sets the KronosUserName.
        /// </summary>
        public string KronosUserName { get; set; }

        /// <summary>
        /// Gets the Teamname for the team that has been mapped.
        /// </summary>
        [Display(Name = "Shifts Team")]
        public List<SelectListItem> ShiftsTeamsId { get; } = new List<SelectListItem>();

        /// <summary>
        /// Gets the UPN of the User in Shifts.
        /// </summary>
        [Display(Name = "Shift User")]
        public List<SelectListItem> ShiftsUsersId { get; } = new List<SelectListItem>();

        /// <summary>
        /// Gets the UPN of the User in Shifts.
        /// </summary>
        [Display(Name = "Kronos User")]
        public List<SelectListItem> KronosUsersId { get; } = new List<SelectListItem>();
    }
}