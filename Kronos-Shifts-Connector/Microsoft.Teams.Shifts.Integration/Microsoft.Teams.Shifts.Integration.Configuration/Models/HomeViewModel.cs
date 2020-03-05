// <copyright file="HomeViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// This class will model the necessary configuration information.
    /// </summary>
    public class HomeViewModel
    {
        /// <summary>
        /// Gets or sets team Id textbox to be used in View.
        /// </summary>
        [Required(ErrorMessage = "Enter Tenant ID.")]
        [MinLength(1)]
        [Display(Name = "Tenant ID")]
        [DataType(DataType.Text)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Workforce Management Provider Name (i.e. KronosWFC).
        /// </summary>
        [Required(ErrorMessage = "Enter the Workforce Management Provider")]
        [MinLength(1)]
        [DataType(DataType.Text)]
        [Display(Name = "Workforce Management System")]
        public string WfmProviderName { get; set; }

        /// <summary>
        /// Gets or sets the Workforce Management Super Username.
        /// </summary>
        [Required(ErrorMessage = "Enter the Kronos Superuser name")]
        [MinLength(1)]
        [DataType(DataType.Text)]
        [Display(Name = "Kronos Superuser Name")]
        public string WfmSuperUsername { get; set; }

        /// <summary>
        /// Gets or sets the Kronos Super User Password.
        /// </summary>
        [Required(ErrorMessage = "Enter the Kronos Superuser password")]
        [MinLength(1)]
        [DataType(DataType.Text)]
        [Display(Name = "Kronos Superuser Password")]
        public string WfmSuperUserPassword { get; set; }

        /// <summary>
        /// Gets or sets the Kronos API Endpoint.
        /// </summary>
        [Required(ErrorMessage = "Enter the Kronos WFC API Endpoint")]
        [MinLength(1)]
        [DataType(DataType.Text)]
        [Display(Name = "Kronos API Endpoint")]
        public string WfmApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the configuration Id.
        /// </summary>
        public string ConfigurationId { get; set; }
    }
}