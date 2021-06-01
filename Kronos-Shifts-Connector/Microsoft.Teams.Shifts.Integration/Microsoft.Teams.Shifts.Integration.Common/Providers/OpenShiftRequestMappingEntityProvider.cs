// <copyright file="OpenShiftRequestMappingEntityProvider.cs" company="Microsoft">
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
    /// The OpenShiftRequestMappingEntityProvider class implementing all methods defined in
    /// <see cref="IOpenShiftRequestMappingEntityProvider"/>.
    /// </summary>
    public class OpenShiftRequestMappingEntityProvider : IOpenShiftRequestMappingEntityProvider
    {
        private const string OpenShiftRequestMappingTableName = "OpenShiftRequestMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable openShiftRequestMappingCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenShiftRequestMappingEntityProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public OpenShiftRequestMappingEntityProvider(
            string connectionString,
            TelemetryClient telemetryClient)
        {
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Saves or updates an open shift request mapping to Azure table storage.
        /// </summary>
        /// <param name="entity">The open shift request mapping entity.</param>
        /// <returns>A unit of execution.</returns>
        public Task SaveOrUpdateOpenShiftRequestMappingEntityAsync(
            AllOpenShiftRequestMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var saveOrUpdateOpenShiftRequestMappingProps = new Dictionary<string, string>()
            {
                { "IncomingShiftRequestId", entity?.RowKey },
                { "IncomingKronosRequestId", entity.KronosOpenShiftRequestId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(
                MethodBase.GetCurrentMethod().Name,
                saveOrUpdateOpenShiftRequestMappingProps);
            return this.StoreOrUpdateEntityAsync(entity);
        }

        /// <summary>
        /// The method that returns the open shift request mapping searching
        /// a Kronos Request ID.
        /// </summary>
        /// <param name="kronosReqId">The Kronos request ID.</param>
        /// <returns>A unit of execution.</returns>
        public async Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByKronosReqIdAsync(string kronosReqId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<AllOpenShiftRequestMappingEntity> query = new TableQuery<AllOpenShiftRequestMappingEntity>();

            // Results list
            List<AllOpenShiftRequestMappingEntity> results = new List<AllOpenShiftRequestMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<AllOpenShiftRequestMappingEntity> queryResults = await this.openShiftRequestMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results.FirstOrDefault(x => x.KronosOpenShiftRequestId == kronosReqId);
        }

        /// <summary>
        /// The method that will get the open shift request mapping entity by matching the Row key.
        /// </summary>
        /// <param name="rowKey">The Row key.</param>
        /// <returns>A unit of execution that contains the AllOpenShiftRequestMappingEntity.</returns>
        public async Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByRowKeyAsync(
            string rowKey)
        {
            var provider = CultureInfo.InvariantCulture;
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CurrentExecutingAssembly", Assembly.GetExecutingAssembly().GetName().Name },
                { "CallingTimestamp", DateTime.UtcNow.ToString("o", provider) },
            };

            this.telemetryClient.TrackEvent("GetOpenShiftRequestMappingEntityByOpenShiftRequestIdAsync", getEntitiesProps);

            // Table query
            TableQuery<AllOpenShiftRequestMappingEntity> query = new TableQuery<AllOpenShiftRequestMappingEntity>();

            // Results list
            List<AllOpenShiftRequestMappingEntity> results = new List<AllOpenShiftRequestMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<AllOpenShiftRequestMappingEntity> queryResults = await this.openShiftRequestMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results.FirstOrDefault(x => x.RowKey == rowKey);
        }

        /// <summary>
        /// Method that will get the open shift request ID using the open shift ID.
        /// </summary>
        /// <param name="openShiftId">The Open Shift ID.</param>
        /// <param name="openShiftReqId">The open shift request id.</param>
        /// <returns>A unit of execution that contains the Open Shift Request entity.</returns>
        public async Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByOpenShiftRequestIdAsync(string openShiftId, string openShiftReqId)
        {
            var provider = CultureInfo.InvariantCulture;
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"GetOpenShiftRequestMappingEntityByOpenShiftIdAsync started at: {DateTime.Now.ToString(provider)} - OpenShiftId: {openShiftId}");

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CurrentExecutingAssembly", Assembly.GetExecutingAssembly().GetName().Name },
                { "CallingTimestamp", DateTime.UtcNow.ToString("O", provider) },
            };

            this.telemetryClient.TrackEvent("GetOpenShiftRequestMappingEntityByOpenShiftIdAsync", getEntitiesProps);

            // Table query
            TableQuery<AllOpenShiftRequestMappingEntity> query = new TableQuery<AllOpenShiftRequestMappingEntity>();
            query.Where(
               TableQuery.CombineFilters(
                   TableQuery.GenerateFilterCondition(
                       "RowKey",
                       QueryComparisons.Equal,
                       openShiftReqId), TableOperators.And,
                   TableQuery.GenerateFilterCondition(
                       "TeamsOpenShiftId",
                       QueryComparisons.Equal,
                       openShiftId)));

            // Results list
            List<AllOpenShiftRequestMappingEntity> results = new List<AllOpenShiftRequestMappingEntity>();
            TableContinuationToken continuationToken = null;

            TableQuerySegment<AllOpenShiftRequestMappingEntity> queryResults = await this.openShiftRequestMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"GetOpenShiftRequestMappingEntityByOpenShiftIdAsync ended at: {DateTime.Now.ToString(provider)} - OpenShiftId: {openShiftId}");

            return queryResults.FirstOrDefault();
        }

        /// <summary>
        /// Method to check for the existance of an open shift inside of the open shift request entity
        /// mapping table.
        /// </summary>
        /// <param name="teamsOpenShiftId">Graph Open Shift ID.</param>
        /// <returns>A unit of execution containing a boolean value indicating whether or not an open shift exists
        /// in the open shift request mapping table.</returns>
        public async Task<bool> CheckOpenShiftRequestExistance(string teamsOpenShiftId)
        {
            var provider = CultureInfo.InvariantCulture;
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"CheckOpenShiftRequestExistance started at: {DateTime.Now.ToString(provider)} - OpenShiftId: {teamsOpenShiftId}");

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CurrentExecutingAssembly", Assembly.GetExecutingAssembly().GetName().Name },
                { "CallingTimestamp", DateTime.UtcNow.ToString("o", provider) },
            };

            this.telemetryClient.TrackEvent("CheckOpenShiftRequestExistance", getEntitiesProps);

            // Table query
            TableQuery<AllOpenShiftRequestMappingEntity> query = new TableQuery<AllOpenShiftRequestMappingEntity>();

            // Results list
            List<AllOpenShiftRequestMappingEntity> results = new List<AllOpenShiftRequestMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                TableQuerySegment<AllOpenShiftRequestMappingEntity> queryResults = await this.openShiftRequestMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            this.telemetryClient.TrackTrace($"CheckOpenShiftRequestExistance ended at: {DateTime.Now.ToString(provider)} - OpenShiftId: {teamsOpenShiftId}");

            return results.Any(x => x.TeamsOpenShiftId == teamsOpenShiftId);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.openShiftRequestMappingCloudTable = cloudTableClient.GetTableReference(OpenShiftRequestMappingTableName);
            await this.openShiftRequestMappingCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }

        private async Task<TableResult> StoreOrUpdateEntityAsync(AllOpenShiftRequestMappingEntity entity)
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

            this.telemetryClient.TrackEvent("StoreOrUpdateEntityAsync", storeOrUpdateEntityProps);

            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.openShiftRequestMappingCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }
    }
}