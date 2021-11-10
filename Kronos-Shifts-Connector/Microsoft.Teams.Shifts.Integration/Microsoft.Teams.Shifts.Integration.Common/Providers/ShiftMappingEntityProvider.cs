// <copyright file="ShiftMappingEntityProvider.cs" company="Microsoft">
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
    /// This class implements all of the methods defined in <see cref="IShiftMappingEntityProvider"/>.
    /// </summary>
    public class ShiftMappingEntityProvider : IShiftMappingEntityProvider
    {
        private const string ShiftMappingTableName = "ShiftEntityMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable shiftEntityMappingCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftMappingEntityProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        public ShiftMappingEntityProvider(TelemetryClient telemetryClient, string connectionString)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
        }

        /// <inheritdoc/>
        public async Task<List<TeamsShiftMappingEntity>> GetAllUsersShiftsByPartitionKeyAsync(string monthPartition, string aadUserId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            string monthPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, monthPartition);
            string aadIdFilter = TableQuery.GenerateFilterCondition("AadUserId", QueryComparisons.Equal, aadUserId);

            // Table query
            TableQuery<TeamsShiftMappingEntity> query = new TableQuery<TeamsShiftMappingEntity>();
            query.Where(TableQuery.CombineFilters(monthPartitionFilter, TableOperators.And, aadIdFilter));

            // Results list
            var results = new List<TeamsShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<TeamsShiftMappingEntity> queryResults = await this.shiftEntityMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Saves or updates the shift mapping entity.
        /// </summary>
        /// <param name="entity">The shift mapping data.</param>
        /// <param name="shiftId">The ShiftID to save or update.</param>
        /// <param name="monthPartitionKey">Month Partition Value.</param>
        /// <returns>A unit of execution.</returns>
        public Task SaveOrUpdateShiftMappingEntityAsync(
            TeamsShiftMappingEntity entity,
            string shiftId,
            string monthPartitionKey)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var saveOrUpdateShiftMappingProps = new Dictionary<string, string>()
            {
                { "IncomingShiftsRequestId", shiftId },
                { "KronosUniqueId", entity?.KronosUniqueId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, saveOrUpdateShiftMappingProps);

            entity.PartitionKey = monthPartitionKey;
            entity.RowKey = shiftId;
            return this.StoreOrUpdateEntityAsync(entity);
        }

        /// <summary>
        /// Method to delete the orphan data from shifts mapping entity.
        /// </summary>
        /// <param name="entity">The mapping entity to be deleted.</param>
        /// <returns>A unit of execution to say whether or not the delete happened successfully.</returns>
        public Task DeleteOrphanDataFromShiftMappingAsync(TeamsShiftMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return this.DeleteEntityAsync(entity);
        }

        /// <summary>
        /// Get all shift mapping entries from table using batch operation.
        /// </summary>
        /// <param name="processKronosUsersInBatchList">List of KronosUserModel.</param>
        /// <param name="monthPartitionKey">Month Partition Value.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>A Task.</returns>
        public async Task<List<TeamsShiftMappingEntity>> GetAllShiftMappingEntitiesInBatchAsync(
            IEnumerable<UserDetailsModel> processKronosUsersInBatchList,
            string monthPartitionKey,
            string queryStartDate,
            string queryEndDate)
        {
            if (processKronosUsersInBatchList is null)
            {
                throw new ArgumentNullException(nameof(processKronosUsersInBatchList));
            }

            var allShiftMappingEntitiesInBatch = new List<TeamsShiftMappingEntity>();
            var task = processKronosUsersInBatchList.Select(async item =>
            {
                var response = await this.GetAllShiftMappingEntitiesInBatchAsync(item, monthPartitionKey, queryStartDate, queryEndDate).ConfigureAwait(false);
                allShiftMappingEntitiesInBatch.AddRange(response);
            });

            await Task.WhenAll(task).ConfigureAwait(false);
            var count = allShiftMappingEntitiesInBatch.Count;

            return allShiftMappingEntitiesInBatch;
        }

        /// <summary>
        /// This method will return a shift mapping entity based upon the RowKey.
        /// </summary>
        /// <param name="tempShiftRowKey">The temporary shift RowKey.</param>
        /// <returns>A unit of execution that contains the TeamsShiftMappingEntity.</returns>
        public async Task<TeamsShiftMappingEntity> GetShiftMappingEntityByRowKeyAsync(string tempShiftRowKey)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(BusinessLogicResource.GetShiftEntityMappingByRowKeyAsync, getEntitiesProps);

            string rowKeyFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, tempShiftRowKey);

            // Table query
            TableQuery<TeamsShiftMappingEntity> query = new TableQuery<TeamsShiftMappingEntity>();
            query.Where(rowKeyFilter);

            // Results list
            var results = new List<TeamsShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<TeamsShiftMappingEntity> queryResults = await this.shiftEntityMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Gets all of the Shift Mapping Entities in a Batch manner.
        /// </summary>
        /// <param name="userModel">The User Model.</param>
        /// <param name="monthPartitionKey">Month Partition Value.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>A unit of execution that contains a list of shift mapping entities.</returns>
        private async Task<List<TeamsShiftMappingEntity>> GetAllShiftMappingEntitiesInBatchAsync(
            UserDetailsModel userModel,
            string monthPartitionKey,
            string queryStartDate,
            string queryEndDate)
        {
            if (userModel is null)
            {
                throw new ArgumentNullException(nameof(userModel));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            CultureInfo culture = CultureInfo.InvariantCulture;

            string userFilter = TableQuery.GenerateFilterCondition("KronosPersonNumber", QueryComparisons.Equal, userModel?.KronosPersonNumber);
            string monthPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, monthPartitionKey);
            string startDateFilter = TableQuery.GenerateFilterConditionForDate("ShiftStartDate", QueryComparisons.GreaterThanOrEqual, Convert.ToDateTime(queryStartDate, culture));
            string endDateFilter = TableQuery.GenerateFilterConditionForDate("ShiftStartDate", QueryComparisons.LessThanOrEqual, Convert.ToDateTime(queryEndDate, culture).AddDays(1));

            // Table query
            TableQuery<TeamsShiftMappingEntity> query = new TableQuery<TeamsShiftMappingEntity>();
            query.Where(TableQuery.CombineFilters(
                TableQuery.CombineFilters(startDateFilter, TableOperators.And, endDateFilter),
                TableOperators.And,
                TableQuery.CombineFilters(monthPartitionFilter, TableOperators.And, userFilter)));

            // Results list
            var results = new List<TeamsShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<TeamsShiftMappingEntity> queryResults = await this.shiftEntityMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        private async Task<TableResult> DeleteEntityAsync(TeamsShiftMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var deleteEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "RecordToDelete", entity.ToString() },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, deleteEntityProps);

            TableOperation deleteOperation = TableOperation.Delete(entity);
            return await this.shiftEntityMappingCloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);
        }

        private async Task<TableResult> StoreOrUpdateEntityAsync(TeamsShiftMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var storeOrUpdateEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, storeOrUpdateEntityProps);

            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.shiftEntityMappingCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.shiftEntityMappingCloudTable = cloudTableClient.GetTableReference(ShiftMappingTableName);
            await this.shiftEntityMappingCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }
    }
}