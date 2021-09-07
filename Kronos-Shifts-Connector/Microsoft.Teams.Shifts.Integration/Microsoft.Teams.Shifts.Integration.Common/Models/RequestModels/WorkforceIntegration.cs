// <copyright file="WorkforceIntegration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels
{
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the request object to be forwarding to Graph API.
    /// </summary>
    public class WorkforceIntegration
    {
        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the apiVersion.
        /// </summary>
        [JsonProperty("apiVersion")]
        public int ApiVersion { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether or not a workforce integration is active.
        /// </summary>
        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the encryption.
        /// </summary>
        [JsonProperty("encryption")]
        public Encryption Encryption { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        [JsonProperty("url")]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url { get; set; }

#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets or sets the functionalities supported for the outbound sync (from Shifts to the WFM provider).
        /// </summary>
        [JsonProperty("supportedEntities")]
        public string SupportedEntities { get; set; }

        /// <summary>
        /// Gets or sets the eligibility functionalities supported for the outbound sync (from Shifts to the WFM provider).
        /// </summary>
        [JsonProperty("eligibilityFilteringEnabledEntities")]
        public string EligibilityFilteringEnabledEntities { get; set; }
    }
}