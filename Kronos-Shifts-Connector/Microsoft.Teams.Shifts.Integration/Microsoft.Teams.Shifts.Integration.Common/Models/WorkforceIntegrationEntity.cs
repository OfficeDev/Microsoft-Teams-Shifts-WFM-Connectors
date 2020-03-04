// <copyright file="WorkforceIntegrationEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the WorkforceIntegrationEntity.
    /// </summary>
    public class WorkforceIntegrationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the Id of the WorkforceIntegration.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the displayName of the WorkforceIntegration.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the apiVersion of the WorkforceIntegration.
        /// </summary>
        [JsonProperty("apiVersion")]
        public int ApiVersion { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether or not a Workforce Integration is active.
        /// </summary>
        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the encryption of the WorkforceIntegration.
        /// </summary>
        [JsonProperty("encryption")]
        public Encryption Encryption { get; set; }

        /// <summary>
        /// Gets or sets the URL of the WorkforceIntegration (NOT Workforce Management endpoint).
        /// </summary>
        [JsonProperty("url")]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets or sets a flag that could have the value of either: none, shift, or swapRequest.
        /// </summary>
        [JsonProperty("supports")]
        public string Supports { get; set; }
    }
}