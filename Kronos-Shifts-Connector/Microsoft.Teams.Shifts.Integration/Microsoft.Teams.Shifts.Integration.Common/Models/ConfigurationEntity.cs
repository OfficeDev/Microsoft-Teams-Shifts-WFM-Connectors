// <copyright file="ConfigurationEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the configuration entity used for storage and retrieval of data in Azure Table storage.
    /// </summary>
    public class ConfigurationEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the ConfigurationId.
        /// </summary>
        [Key]
        [JsonProperty("ConfigurationId")]
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        [JsonProperty("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the WfmProvider (Workforce Management Provider).
        /// </summary>
        [JsonProperty("WfmProvider")]
        public string WfmProvider { get; set; } = "KronosWFC";

        /// <summary>
        /// Gets or sets the WfmApiEndpoint (Workforce Management API Endpoint).
        /// </summary>
        [JsonProperty("WfmApiEndpoint")]
        public string WfmApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the WorkforceIntegrationId.
        /// </summary>
        [JsonProperty("WorkforceIntegrationId")]
        public string WorkforceIntegrationId { get; set; }

        /// <summary>
        /// Gets or sets the WorkforceIntegrationSecret.
        /// </summary>
        [JsonProperty("WorkforceIntegrationSecret")]
        public string WorkforceIntegrationSecret { get; set; }

        /// <summary>
        /// Gets or sets the AdminAadObjectId - the AAD object ID of the user signed in.
        /// </summary>
        [JsonProperty("AdminAadObjectId")]
        public string AdminAadObjectId { get; set; }
    }
}