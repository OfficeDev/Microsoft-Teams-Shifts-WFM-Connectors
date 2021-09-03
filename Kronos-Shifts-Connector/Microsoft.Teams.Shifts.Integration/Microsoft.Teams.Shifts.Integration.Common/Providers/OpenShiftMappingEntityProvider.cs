// <copyright file="OpenShiftMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements methods that are defined in <see cref="IOpenShiftMappingEntityProvider"/>.
    /// </summary>
    public class OpenShiftMappingEntityProvider : IOpenShiftMappingEntityProvider
    {
        private const string OpenShiftMappingTableName = "OpenShiftEntityMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable openShiftEntityMappingCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenShiftMappingEntityProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        public OpenShiftMappingEntityProvider(
            TelemetryClient telemetryClient,
            string connectionString)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
        }

        /// <summary>
        /// Method implementation to be able to store or update the OpenShiftMapppingEntity.
        /// </summary>
        /// <param name="entity">The open shift mapping entity.</param>
        /// <returns>A unit of execution.</returns>
        public async Task SaveOrUpdateOpenShiftMappingEntityAsync(
            AllOpenShiftMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var saveOrUpdateShiftMappingProps = new Dictionary<string, string>()
            {
                { "TeamsOpenShiftId", entity.TeamsOpenShiftId },
                { "KronosSlots", entity.KronosSlots },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, saveOrUpdateShiftMappingProps);
            await this.StoreOrUpdateEntityAsync(entity).ConfigureAwait(false);
        }

        /// <summary>
        /// Method implementation to be able to return all entries for the OpenShiftEntityMapping table.
        /// </summary>
        /// <param name="openShiftId">The open shift Id.</param>
        /// <returns>A list of all the entities in the Azure table.</returns>
        public async Task<List<AllOpenShiftMappingEntity>> GetOpenShiftMappingEntitiesAsync(string openShiftId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<AllOpenShiftMappingEntity> query = new TableQuery<AllOpenShiftMappingEntity>();
            query.Where(TableQuery.GenerateFilterCondition("TeamsOpenShiftId", QueryComparisons.Equal, openShiftId));

            // Results list
            List<AllOpenShiftMappingEntity> results = new List<AllOpenShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<AllOpenShiftMappingEntity> queryResults = await this.openShiftEntityMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Method to delete the orphan data from open shifts mapping entity.
        /// </summary>
        /// <param name="entity">The mapping entity to be deleted.</param>
        /// <returns>A unit of execution to say whether or not the delete happened successfully.</returns>
        public async Task DeleteOrphanDataFromOpenShiftMappingAsync(AllOpenShiftMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await this.DeleteEntityAsync(entity).ConfigureAwait(false);
        }

        /// <summary>
        /// Method to get all shift mapping entities.
        /// </summary>
        /// <param name="monthPartitionKey">The month partition key.</param>
        /// <param name="orgJobPath">The org job path.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>Gets a list of all the open shift mapping entities in the batch.</returns>
        public async Task<List<AllOpenShiftMappingEntity>> GetAllOpenShiftMappingEntitiesInBatch(
            string monthPartitionKey,
            string orgJobPath,
            string queryStartDate,
            string queryEndDate)
        {
            var allOpenShiftMappingEntities = await this.GetAllOpenShiftMappingEntitiesInBatchAsync(monthPartitionKey, orgJobPath, queryStartDate, queryEndDate).ConfigureAwait(false);
            return allOpenShiftMappingEntities;
        }

        /// <summary>
        /// This method deletes the open shift by the open shift ID from the database.
        /// </summary>
        /// <param name="openShiftId">The open shift ID.</param>
        /// <returns>A unit of execution.</returns>
        public async Task DeleteOrphanDataFromOpenShiftMappingByOpenShiftIdAsync(string openShiftId)
        {
            var openShiftEntitiesById = await this.GetOpenShiftMappingEntitiesAsync(openShiftId).ConfigureAwait(false);
            var openShiftEntityToDelete = openShiftEntitiesById?.FirstOrDefault();
            await this.DeleteEntityAsync(openShiftEntityToDelete).ConfigureAwait(false);
        }

        /// <summary>
        /// This method gets all Open Shift Mapping entities in batch.
        /// </summary>
        /// <param name="monthPartitionKey">The month wise partition key.</param>
        /// <param name="orgJobPath">The orgJobPath.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>A unit of execution that contains a list of open shift mapping entities.</returns>
        private async Task<List<AllOpenShiftMappingEntity>> GetAllOpenShiftMappingEntitiesInBatchAsync(
            string monthPartitionKey,
            string orgJobPath,
            string queryStartDate,
            string queryEndDate)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            CultureInfo culture = CultureInfo.InvariantCulture;

            string monthPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, monthPartitionKey);
            string orgJobPathFilter = TableQuery.GenerateFilterCondition("OrgJobPath", QueryComparisons.Equal, orgJobPath);
            string startDateFilter = TableQuery.GenerateFilterConditionForDate("OpenShiftStartDate", QueryComparisons.GreaterThanOrEqual, Convert.ToDateTime(queryStartDate, culture));
            string endDateFilter = TableQuery.GenerateFilterConditionForDate("OpenShiftStartDate", QueryComparisons.LessThanOrEqual, Convert.ToDateTime(queryEndDate, culture).AddDays(1));

            // Table query
            TableQuery<AllOpenShiftMappingEntity> query = new TableQuery<AllOpenShiftMappingEntity>();

            query.Where(TableQuery.CombineFilters(
                TableQuery.CombineFilters(startDateFilter, TableOperators.And, endDateFilter),
                TableOperators.And,
                TableQuery.CombineFilters(monthPartitionFilter, TableOperators.And, orgJobPathFilter)));

            // Results list
            var results = new List<AllOpenShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<AllOpenShiftMappingEntity> queryResults = await this.openShiftEntityMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        private async Task<TableResult> DeleteEntityAsync(AllOpenShiftMappingEntity entity)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var deleteEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "RecordToDelete", entity.ToString() },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, deleteEntityProps);

            TableOperation deleteOperation = TableOperation.Delete(entity);
            return await this.openShiftEntityMappingCloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);
        }

        private async Task<TableResult> StoreOrUpdateEntityAsync(AllOpenShiftMappingEntity entity)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var storeOrUpdateEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, storeOrUpdateEntityProps);

            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.openShiftEntityMappingCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.openShiftEntityMappingCloudTable = cloudTableClient.GetTableReference(OpenShiftMappingTableName);
            await this.openShiftEntityMappingCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }
    }
}