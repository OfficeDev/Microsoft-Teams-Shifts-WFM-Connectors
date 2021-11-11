// <copyright file="TeamDepartmentMappingProvider.cs" company="Microsoft">
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
    using Microsoft.Graph;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements all of the methods defined in <see cref="ITeamDepartmentMappingProvider"/>.
    /// </summary>
    public class TeamDepartmentMappingProvider : ITeamDepartmentMappingProvider
    {
        private const string TeamDepartmentMappingTableName = "TeamToDepartmentWithJobMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable teamDepartmentMappingCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDepartmentMappingProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Table storage connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public TeamDepartmentMappingProvider(string connectionString, TelemetryClient telemetryClient)
        {
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.telemetryClient = telemetryClient;
        }

        /// <inheritdoc/>
        public async Task<TeamToDepartmentJobMappingEntity> GetTeamMappingForOrgJobPathAsync(
            string workForceIntegrationId,
            string orgJobPath)
        {
            var getTeamDepartmentMappingProps = new Dictionary<string, string>()
            {
                { "QueryingOrgJobPath", orgJobPath },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", getTeamDepartmentMappingProps);

            try
            {
                await this.EnsureInitializedAsync().ConfigureAwait(false);
                var searchOperation = TableOperation.Retrieve<TeamToDepartmentJobMappingEntity>(workForceIntegrationId, orgJobPath);
                TableResult searchResult = await this.teamDepartmentMappingCloudTable.ExecuteAsync(searchOperation).ConfigureAwait(false);
                var result = (TeamToDepartmentJobMappingEntity)searchResult.Result;
                return result;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                return null;
                throw;
            }
        }

        /// <summary>
        /// Function that will return all of the teams that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped teams.</returns>
        public async Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamDetailsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamToDepartmentJobMappingEntity> query = new TableQuery<TeamToDepartmentJobMappingEntity>();

            // Results list
            List<TeamToDepartmentJobMappingEntity> results = new List<TeamToDepartmentJobMappingEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<TeamToDepartmentJobMappingEntity> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <inheritdoc/>
        public async Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamDetailsBySchedulingGroupAsync(string teamId, string schedulingGroupId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamToDepartmentJobMappingEntity> query = new TableQuery<TeamToDepartmentJobMappingEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("TeamId", QueryComparisons.Equal, teamId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("TeamsScheduleGroupId", QueryComparisons.Equal, schedulingGroupId)));

            // Results list
            return await this.GetQueryResultsAsync(query).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is to make sure to get all the records from the TeamToDepartmentJobMappingEntity table.
        /// </summary>
        /// <returns>A list of TeamToDepartmentJobMappingEntity.</returns>
        public async Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamToDeptsWithJobPathsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamToDepartmentJobMappingEntity> query = new TableQuery<TeamToDepartmentJobMappingEntity>();

            // Results list
            return await this.GetQueryResultsAsync(query).ConfigureAwait(false);
        }

        /// <summary>
        /// Function that will return all the mappings for a single team that are mapped in Azure Table storage.
        /// </summary>
        /// <param name="teamId">The ID of the team to get the mappings for.</param>
        /// <returns>The mappings for the team.</returns>
        public async Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamDetailsAsync(string teamId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamToDepartmentJobMappingEntity> query = new TableQuery<TeamToDepartmentJobMappingEntity>()
                .Where(TableQuery.GenerateFilterCondition("TeamId", QueryComparisons.Equal, teamId));

            // Results list
            return await this.GetQueryResultsAsync(query).ConfigureAwait(false);
        }

        /// <summary>
        /// Method that will get all the org job paths.
        /// </summary>
        /// <returns>A list of org job paths.</returns>
        public async Task<List<string>> GetAllOrgJobPathsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            List<string> orgJobPathList = new List<string>();

            var results = await this.GetMappedTeamDetailsAsync().ConfigureAwait(false);
            foreach (var item in results)
            {
                orgJobPathList.Add(item.RowKey.Replace('$', '/'));
            }

            return orgJobPathList;
        }

        /// <summary>
        /// Method to save or update Teams to Department mapping.
        /// </summary>
        /// <param name="entity">Mapping entity reference.</param>
        /// <returns>http status code representing the asynchronous operation.</returns>
        public async Task<bool> SaveOrUpdateTeamsToDepartmentMappingAsync(TeamToDepartmentJobMappingEntity entity)
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
        /// Method to delete teams and Department mapping.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>boolean value that indicates delete success.</returns>
        public async Task<bool> DeleteMappedTeamDeptDetailsAsync(string partitionKey, string rowKey)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamToDepartmentJobMappingEntity> deleteQuery = new TableQuery<TeamToDepartmentJobMappingEntity>();
            deleteQuery.Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));

            TableContinuationToken continuationToken = null;
            if (await this.teamDepartmentMappingCloudTable.ExistsAsync().ConfigureAwait(false))
            {
                TableQuerySegment<TeamToDepartmentJobMappingEntity> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(
                    deleteQuery, continuationToken).ConfigureAwait(false);

                continuationToken = queryResults.ContinuationToken;
                var row = queryResults.Results.FirstOrDefault();

                TableOperation delete = TableOperation.Delete(row);

                var result = await this.teamDepartmentMappingCloudTable.ExecuteAsync(delete).ConfigureAwait(false);
                if (result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<TableResult> StoreOrUpdateEntityAsync(TableEntity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var storeOrUpdateEntityProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "ExecutingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, storeOrUpdateEntityProps);

            TableOperation addOrUpdateOperation = TableOperation.InsertOrReplace(entity);
            return await this.teamDepartmentMappingCloudTable.ExecuteAsync(addOrUpdateOperation).ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.teamDepartmentMappingCloudTable = cloudTableClient.GetTableReference(TeamDepartmentMappingTableName);
            await this.teamDepartmentMappingCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the full set of mapped team departments determined by the query.
        /// </summary>
        /// <param name="query">The query to use when fetching the mapped team departments.</param>
        /// <returns>The full set of mapped team departments.</returns>
        private async Task<List<TeamToDepartmentJobMappingEntity>> GetQueryResultsAsync(TableQuery<TeamToDepartmentJobMappingEntity> query)
        {
            List<TeamToDepartmentJobMappingEntity> results = new List<TeamToDepartmentJobMappingEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<TeamToDepartmentJobMappingEntity> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }
    }
}