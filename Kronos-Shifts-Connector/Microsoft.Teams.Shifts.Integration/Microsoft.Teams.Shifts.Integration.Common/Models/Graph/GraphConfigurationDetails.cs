// <copyright file="GraphConfigurationDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.Graph
{
    /// <summary>
    /// Contains all the necessary configuration details to enable Graph requests.
    /// </summary>
    public class GraphConfigurationDetails
    {
        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the ClientId.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the ClientSecret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the Instance.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Gets or sets the AAD Object Id of the Shifts Admin.
        /// </summary>
        public string ShiftsAdminAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets the MS Graph Access token.
        /// </summary>
        public string ShiftsAccessToken { get; set; }
    }
}