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

        /// <summary>
        /// Retrieves a single TeamDepartmentMapping from Azure Table storage.
        /// </summary>
        /// <param name="workForceIntegrationId">WorkForceIntegration Id.</param>
        /// <param name="orgJobPath">Kronos Org Job Path.</param>
        /// <returns>A unit of execution that boxes a <see cref="ShiftsTeamDepartmentMappingEntity"/>.</returns>
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
        /// Saving or updating a mapping between a Team in Shifts and Department in Kronos.
        /// </summary>
        /// <param name="shiftsTeamsDetails">Shifts team details fetched via Graph api calls.</param>
        /// <param name="kronosDepartmentName">Department name fetched from Kronos.</param>
        /// <param name="workforceIntegrationId">The Workforce Integration Id.</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>A unit of execution.</returns>
        public async Task<bool> SaveOrUpdateShiftsTeamDepartmentMappingAsync(
            Team shiftsTeamsDetails,
            string kronosDepartmentName,
            string workforceIntegrationId,
            string tenantId)
        {
            try
            {
                var saveOrUpdateShiftsProps = new Dictionary<string, string>()
                {
                    { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                    { "ExecutingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                };

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, saveOrUpdateShiftsProps);

                // TODO: The following code is for experimental purpose and incomplete, will change the code once the blockers are removed.
                var entity = new ShiftsTeamDepartmentMappingEntity()
                {
                    PartitionKey = tenantId,
                    RowKey = kronosDepartmentName,
                    ShiftsTeamName = shiftsTeamsDetails?.DisplayName,
                    TeamId = shiftsTeamsDetails.Id,
                    WorkforceIntegrationId = workforceIntegrationId,
                    IsArchived = shiftsTeamsDetails.IsArchived ?? false,
                    TeamDescription = shiftsTeamsDetails.Description,
                    TeamInternalId = shiftsTeamsDetails.InternalId,
                    TeamUrl = shiftsTeamsDetails.WebUrl,
                };

                var result = await this.StoreOrUpdateEntityAsync(entity).ConfigureAwait(false);
                return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                return false;
                throw;
            }
        }

        /// <summary>
        /// Function that will return all of the teams that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped teams.</returns>
        public async Task<List<ShiftsTeamDepartmentMappingEntity>> GetMappedTeamDetailsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<ShiftsTeamDepartmentMappingEntity> query = new TableQuery<ShiftsTeamDepartmentMappingEntity>();

            // Results list
            List<ShiftsTeamDepartmentMappingEntity> results = new List<ShiftsTeamDepartmentMappingEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<ShiftsTeamDepartmentMappingEntity> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
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

        /// <summary>
        /// Method that will get all the org job paths.
        /// </summary>
        /// <returns>A list of org job paths.</returns>
        public async Task<List<string>> GetAllOrgJobPathsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            List<string> orgJobPathList = new List<string>();

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
        public async Task<bool> TeamsToDepartmentMappingAsync(TeamsDepartmentMappingModel entity)
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
        /// Function that will return all of the teams and department that are
        /// mapped in Azure Table storage.
        /// </summary>
        /// <returns>List of Team and Department mapping model.</returns>
        public async Task<List<TeamsDepartmentMappingModel>> GetTeamDeptMappingDetailsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<TeamsDepartmentMappingModel> query = new TableQuery<TeamsDepartmentMappingModel>();

            // Results list
            List<TeamsDepartmentMappingModel> results = new List<TeamsDepartmentMappingModel>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<TeamsDepartmentMappingModel> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
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
            TableQuery<TeamsDepartmentMappingModel> deleteQuery = new TableQuery<TeamsDepartmentMappingModel>();
            deleteQuery
            .Where(TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));

            TableContinuationToken continuationToken = null;
            if (await this.teamDepartmentMappingCloudTable.ExistsAsync().ConfigureAwait(false))
            {
                TableQuerySegment<TeamsDepartmentMappingModel> queryResults = await this.teamDepartmentMappingCloudTable.ExecuteQuerySegmentedAsync(
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
    }
}