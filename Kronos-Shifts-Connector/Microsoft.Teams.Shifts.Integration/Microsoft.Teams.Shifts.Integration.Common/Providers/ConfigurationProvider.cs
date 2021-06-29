// <copyright file="ConfigurationProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements the methods defined in <see cref="IConfigurationProvider"/>.
    /// </summary>
    public class ConfigurationProvider : IConfigurationProvider
    {
        private const string ConfigurationTableName = "ConfigurationInfo";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable configurationCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public ConfigurationProvider(string connectionString, TelemetryClient telemetryClient)
        {
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Saves or updates a configuration entity.
        /// </summary>
        /// <param name="configuration">The configuration data.</param>
        /// <returns>A unit of execution.</returns>
        public Task SaveOrUpdateConfigurationAsync(ConfigurationEntity configuration)
        {
            var saveOrUpdateConfigProps = new Dictionary<string, string>()
            {
                { "IncomingConfigurationId", configuration?.ConfigurationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, saveOrUpdateConfigProps);

            configuration.PartitionKey = configuration.TenantId;
            configuration.RowKey = configuration.ConfigurationId;
            return this.StoreOrUpdateEntityAsync(configuration);
        }

        /// <summary>
        /// Method to delete the configuration entity.
        /// </summary>
        /// <param name="entity">The configuration to be deleted.</param>
        /// <returns>A unit of execution to say whether or not the delete happened successfully.</returns>
        public Task DeleteConfigurationAsync(ConfigurationEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return this.DeleteEntityAsync(entity);
        }

        /// <summary>
        /// Retrieves a configuration that has been saved.
        /// </summary>
        /// <param name="tenantId">The tenantId.</param>
        /// <param name="configurationId">The name of the workforce provider.</param>
        /// <returns>A unit of execution that contains a configuration entity.</returns>
        public async Task<ConfigurationEntity> GetConfigurationAsync(string tenantId, string configurationId)
        {
            var getConfigurationProps = new Dictionary<string, string>()
            {
                { "QueryingTenantId", tenantId },
                { "QueryingConfigurationId", configurationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getConfigurationProps);

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            var searchOperation = TableOperation.Retrieve<ConfigurationEntity>(tenantId, configurationId);
            var searchResult = await this.configurationCloudTable.ExecuteAsync(searchOperation).ConfigureAwait(false);
            return (ConfigurationEntity)searchResult.Result;
        }

        /// <summary>
        /// Function that will return all of the configurations that are registered in Azure Table storage.
        /// </summary>
        /// <returns>A list of the configurations established.</returns>
        public async Task<List<ConfigurationEntity>> GetConfigurationsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getConfigurationsProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace("Getting configurations.", getConfigurationsProps);

            // Table query
            TableQuery<ConfigurationEntity> query = new TableQuery<ConfigurationEntity>();

            // Results list
            List<ConfigurationEntity> results = new List<ConfigurationEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<ConfigurationEntity> queryResults = await this.configurationCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Creates the Azure table if the table does not already exist.
        /// </summary>
        /// <param name="connectionString">The connection string for the Azure storage.</param>
        /// <returns>A unit of execution.</returns>
        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.configurationCloudTable = cloudTableClient.GetTableReference(ConfigurationTableName);
            await this.configurationCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// The actual operation of adding or updating a record in Azure Table storage.
        /// </summary>
        /// <param name="entity">The entity to add or update.</param>
        /// <returns>A unit of execution that has the new record boxed in.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(ConfigurationEntity entity)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var storeOrUpdateEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, storeOrUpdateEntityProps);

            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.configurationCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }

        private async Task<TableResult> DeleteEntityAsync(ConfigurationEntity entity)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var deleteEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "RecordToDelete", entity.ToString() },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, deleteEntityProps);

            TableOperation deleteOperation = TableOperation.Delete(entity);
            return await this.configurationCloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);
        }

        /// <summary>
        /// Having the necessary entities properly created.
        /// </summary>
        /// <returns>A unit of execution.</returns>
        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }
    }
}