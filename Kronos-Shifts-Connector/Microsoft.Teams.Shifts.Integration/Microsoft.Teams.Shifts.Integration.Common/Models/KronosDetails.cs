// <copyright file="KronosDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    /// <summary>
    /// This class models the KronosDetails.
    /// </summary>
    public class KronosDetails
    {
        /// <summary>
        /// Gets or sets the Workforce Integration Id.
        /// </summary>
        public string WorkforceIntegrationId { get; set; }

        /// <summary>
        /// Gets or sets the KronosOrgJobPath.
        /// </summary>
        public string KronosOrgJobPath { get; set; }
    }
}