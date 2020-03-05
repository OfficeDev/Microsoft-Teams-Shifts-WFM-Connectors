// <copyright file="IConfigurationProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// Interface of the configuration provider.
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Method definition to save or update data in the Azure table storage.
        /// </summary>
        /// <param name="configuration">The configuration data to save or update.</param>
        /// <returns>A value indicating whether or not the data was saved successfully.</returns>
        Task SaveOrUpdateConfigurationAsync(ConfigurationEntity configuration);

        /// <summary>
        /// Method to delete the configuration entity.
        /// </summary>
        /// <param name="configuration">The configuration entity to delete.</param>
        /// <returns>A unit of execution.</returns>
        Task DeleteConfigurationAsync(ConfigurationEntity configuration);

        /// <summary>
        /// Method definition for retrieving the necessary data from the Azure table storage.
        /// </summary>
        /// <param name="tenantId">The partition key.</param>
        /// <param name="configurationId">The row key.</param>
        /// <returns>The details for a specific configuration.</returns>
        Task<ConfigurationEntity> GetConfigurationAsync(string tenantId, string configurationId);

        /// <summary>
        /// Method to get all of the saved data.
        /// </summary>
        /// <returns>A list of configurations that have been saved.</returns>
        Task<List<ConfigurationEntity>> GetConfigurationsAsync();
    }
}