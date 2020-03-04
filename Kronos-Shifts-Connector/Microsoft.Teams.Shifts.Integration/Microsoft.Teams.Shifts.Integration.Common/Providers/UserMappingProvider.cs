// <copyright file="UserMappingProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements the methods defined in <see cref="IUserMappingProvider"/>.
    /// </summary>
    public class UserMappingProvider : IUserMappingProvider
    {
        private const string UserMappingTableName = "UserToUserMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable userMappingCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMappingProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Table storage connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public UserMappingProvider(string connectionString, TelemetryClient telemetryClient)
        {
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Function that will return all of the Users that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped Users.</returns>
        public async Task<List<AllUserMappingEntity>> GetAllMappedUserDetailsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<AllUserMappingEntity> query = new TableQuery<AllUserMappingEntity>();

            // Results list
            List<AllUserMappingEntity> results = new List<AllUserMappingEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<AllUserMappingEntity> queryResults = await this.userMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Method to get a single user mapping entity.
        /// </summary>
        /// <param name="userAadObjectId">The AAD Object ID of the user.</param>
        /// <param name="teamId">The TeamID that the user belongs to.</param>
        /// <returns>A unit of execution that contains the UserMappingEntity.</returns>
        public async Task<AllUserMappingEntity> GetUserMappingEntityAsyncNew(string userAadObjectId, string teamId)
        {
            var getUserEntityMappingProps = new Dictionary<string, string>()
            {
                { "QueryAadObjectId", userAadObjectId },
                { "QueryTeamId", teamId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getUserEntityMappingProps);
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<AllUserMappingEntity> query = new TableQuery<AllUserMappingEntity>();
            query.Where(
                    TableQuery.GenerateFilterCondition(
                        "ShiftUserAadObjectId",
                        QueryComparisons.Equal,
                        userAadObjectId));

            // Results list
            List<AllUserMappingEntity> results = new List<AllUserMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.userMappingCloudTable.ExistsAsync().ConfigureAwait(false))
            {
                    TableQuerySegment<AllUserMappingEntity> queryResults = await this.userMappingCloudTable.ExecuteQuerySegmentedAsync(
                        query,
                        continuationToken).ConfigureAwait(false);

                    continuationToken = queryResults.ContinuationToken;
                    results = queryResults.Results;
            }

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Method to save or update the user mapping.
        /// </summary>
        /// <param name="entity">Mapping entity reference.</param>
        /// <returns>http status code representing the asynchronous operation.</returns>
        public async Task<bool> KronosShiftUsersMappingAsync(AllUserMappingEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                var result = await this.StoreOrUpdateEntityAsync(entity).ConfigureAwait(false);
                return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        /// <summary>
        /// Method to get distinct org job path.
        /// </summary>
        /// <returns>A list of distinct org job path.</returns>
        public async Task<List<string>> GetDistinctOrgJobPatAsync()
        {
            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name);
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<AllUserMappingEntity> query = new TableQuery<AllUserMappingEntity>();
            query.Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey",
                        QueryComparisons.NotEqual,
                        string.Empty));

            // Results list
            List<AllUserMappingEntity> results = new List<AllUserMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.userMappingCloudTable.ExistsAsync().ConfigureAwait(false))
            {
                TableQuerySegment<AllUserMappingEntity> queryResults = await this.userMappingCloudTable.ExecuteQuerySegmentedAsync(
                    query,
                    continuationToken).ConfigureAwait(false);

                continuationToken = queryResults.ContinuationToken;
                results = queryResults.Results;
            }

            return results.Select(x => x.PartitionKey).Distinct().ToList();
        }

        /// <summary>
        /// Method to delete user mapping.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>boolean value that indicates delete success.</returns>
        public async Task<bool> DeleteMappedUserDetailsAsync(string partitionKey, string rowKey)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<AllUserMappingEntity> deleteQuery = new TableQuery<AllUserMappingEntity>();
            deleteQuery
            .Where(TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));

            // Results list
            List<AllUserMappingEntity> results = new List<AllUserMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.userMappingCloudTable.ExistsAsync().ConfigureAwait(false))
            {
                TableQuerySegment<AllUserMappingEntity> queryResults = await this.userMappingCloudTable.ExecuteQuerySegmentedAsync(
                    deleteQuery,
                    continuationToken).ConfigureAwait(false);

                continuationToken = queryResults.ContinuationToken;
                results = queryResults.Results;
            }

            var row = results.FirstOrDefault();
            TableOperation delete = TableOperation.Delete(row);

            var result = await this.userMappingCloudTable.ExecuteAsync(delete).ConfigureAwait(false);

            if (result.HttpStatusCode == (int)HttpStatusCode.NoContent)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// The actual operation of adding or updating a record in Azure Table storage.
        /// </summary>
        /// <param name="entity">The entity to add or update.</param>
        /// <returns>A unit of execution that has the new record boxed in.</returns>
        private async Task<TableResult> StoreOrUpdateEntityAsync(TableEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.userMappingCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }

        /// <summary>
        /// Having the necessary entities properly created.
        /// </summary>
        /// <returns>A unit of execution.</returns>
        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value.ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.userMappingCloudTable = cloudTableClient.GetTableReference(UserMappingTableName);
            await this.userMappingCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}