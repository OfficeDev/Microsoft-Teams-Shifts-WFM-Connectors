// <copyright file="ShiftsTeamKronosDepartmentViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Newtonsoft.Json;

    /// <summary>
    /// This class represents the model for the Shifts Team Name.
    /// </summary>
    public class ShiftsTeamKronosDepartmentViewModel
    {
        /// <summary>
        /// Gets or sets the Shifts Team Name.
        /// </summary>
        [JsonProperty("TeamName")]
        [Display(Name = "Team")]
        public string ShiftsTeamName { get; set; }

        /// <summary>
        /// Gets or sets the Kronos Department Name.
        /// </summary>
        [JsonProperty("DepartmentName")]
        [Display(Name = "Department")]
        public string KronosDepartmentName { get; set; }

        /// <summary>
        /// Gets the Kronos Departments.
        /// </summary>
        public List<SelectListItem> KronosDepartments { get; } = new List<SelectListItem>();

        /// <summary>
        /// Gets the Shifts Teams.
        /// </summary>
        public List<SelectListItem> ShiftTeams { get; } = new List<SelectListItem>();

        /// <summary>
        /// Gets or sets the TeamId.
        /// </summary>
        [JsonProperty("GraphGroupId")]
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Channel Id.
        /// </summary>
        [JsonProperty("GeneralChannelId")]
        public string TeamInternalId { get; set; }
    }
}