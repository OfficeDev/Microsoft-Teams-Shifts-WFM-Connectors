// <copyright file="SetupDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.Graph;

    /// <summary>
    /// This class models the SetupDetails.
    /// </summary>
    public class SetupDetails
    {
        /// <summary>
        /// Gets or sets the ErrorMessage.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the WFI Id.
        /// </summary>
        public string WFIId { get; set; }

        /// <summary>
        /// Gets or sets the KronosSession.
        /// </summary>
        public string KronosSession { get; set; }

        /// <summary>
        /// Gets or sets the Kronos API Endpoint.
        /// </summary>
        public string WfmEndPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the prerequisites have been established.
        /// </summary>
        public bool IsAllSetUpExists { get; set; }

        /// <summary>
        /// Gets or sets the Kronos User Name.
        /// </summary>
        public string KronosUserName { get; set; }

        /// <summary>
        /// Gets or sets the Kronos Password.
        /// </summary>
        public string KronosPassword { get; set; }

        /// <summary>
        /// Gets or sets the GraphConfigurationDetails.
        /// </summary>
        public GraphConfigurationDetails GraphConfigurationDetails { get; set; }
    }
}