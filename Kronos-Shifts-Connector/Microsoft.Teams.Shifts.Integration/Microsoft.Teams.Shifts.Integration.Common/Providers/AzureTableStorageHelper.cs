// <copyright file="AzureTableStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Azure table storage helper class.
    /// </summary>
    [Serializable]
    public class AzureTableStorageHelper : IAzureTableStorageHelper
    {
        private readonly string connectionString;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableStorageHelper" /> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public AzureTableStorageHelper(string connectionString, TelemetryClient telemetryClient)
        {
            this.connectionString = connectionString;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Gets cloud table client.
        /// </summary>
        private CloudTableClient TableClient { get => this.StorageAccount.CreateCloudTableClient(); }

        /// <summary>
        /// Gets the field to access storage account.
        /// </summary>
        private CloudStorageAccount StorageAccount { get => CloudStorageAccount.Parse(this.connectionString); }

        /// <summary>
        /// Insert or merge the activity Id.
        /// </summary>
        /// <typeparam name="T">The table entity.</typeparam>
        /// <param name="entity">The table entity to merge or insert.</param>
        /// <param name="tableName">The name of the Azure Table.</param>
        /// <returns>A unit of execution.</returns>
        public async Task<T> InsertOrMergeTableEntityAsync<T>(T entity, string tableName)
            where T : TableEntity
        {
            var activityEntity = default(T);

            var insertOrMergeTableProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, insertOrMergeTableProps);

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                CloudTable table = this.TableClient.GetTableReference(tableName);
                await table.CreateIfNotExistsAsync().ConfigureAwait(false);

                TableOperation insertOrMergeOperation = TableOperation.InsertOrReplace(entity);

                TableResult result = await table.ExecuteAsync(insertOrMergeOperation).ConfigureAwait(false);
                activityEntity = result.Result as T;
            }
            catch (StorageException ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return activityEntity;
        }

        /// <summary>
        /// This method will fetch records from an Azure table.
        /// </summary>
        /// <typeparam name="T">A generic type.</typeparam>
        /// <param name="tableName">The name of the Azure table.</param>
        /// <param name="partitionKey">The partition key of that table.</param>
        /// <returns>A list of type T that is boxed in a unit of execution.</returns>
        public async Task<List<T>> FetchTableRecordsAsync<T>(string tableName, string partitionKey)
            where T : ITableEntity, new()
        {
            CloudTable table = this.TableClient.GetTableReference(tableName);

            if (await table.CreateIfNotExistsAsync().ConfigureAwait(false))
            {
                return null;
            }
            else
            {
                TableQuery<T> query = new TableQuery<T>();
                if (tableName != "UserToUserMapping")
                {
                    query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
                }

                // Results list
                List<T> results = new List<T>();
                TableContinuationToken continuationToken = null;

                do
                {
                    var queryResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                    continuationToken = queryResults.ContinuationToken;
                    results.AddRange(queryResults.Results);
                }
                while (continuationToken != null);

                return results;
            }
        }
    }
}