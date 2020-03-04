// <copyright file="ConfigEntityViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    /// <summary>
    /// Represents the configuration entity used to bind data to datatable in Home Page.
    /// </summary>
    public class ConfigEntityViewModel
    {
        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the ConfigurationId.
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the WfmSuperUsername (Workforce Management Super Username).
        /// </summary>
        public string WfmSuperUsername { get; set; }

        /// <summary>
        /// Gets or sets the WfmSuperUserPassword (Workforce Management Super User Password).
        /// </summary>
        public string WfmSuperUserPassword { get; set; }

        /// <summary>
        /// Gets or sets the WfmApiEndpoint (Workforce Management API Endpoint).
        /// </summary>
        public string WfmApiEndpoint { get; set; }
    }
}
