// <copyright file="ShiftsTeamDepartmentMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the Team-Department mapping.
    /// </summary>
    public class ShiftsTeamDepartmentMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the workforceIntegrationId.
        /// </summary>
        public string WorkforceIntegrationId { get; set; }

        /// <summary>
        /// Gets or sets the TeamId.
        /// </summary>
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Team name in Shifts.
        /// </summary>
        public string ShiftsTeamName { get; set; }

        /// <summary>
        /// Gets or sets the Team description.
        /// </summary>
        public string TeamDescription { get; set; }

        /// <summary>
        /// Gets or sets the Team description.
        /// </summary>
        public string TeamInternalId { get; set; }

        /// <summary>
        /// Gets or sets the Team url.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string TeamUrl { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets or sets a value indicating whether the status of Shifts team is archived.
        /// </summary>
        public bool IsArchived { get; set; }
    }
}