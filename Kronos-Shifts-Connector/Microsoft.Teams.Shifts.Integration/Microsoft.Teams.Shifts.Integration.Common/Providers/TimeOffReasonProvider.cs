// <copyright file="TimeOffReasonProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements the methods defined in <see cref="ITimeOffReasonProvider"/>.
    /// </summary>
    public class TimeOffReasonProvider : ITimeOffReasonProvider
    {
        private const string ConfigurationTableName = "PayCodeToTimeOffReasonsMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable timeOffReasonsCloudTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffReasonProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        public TimeOffReasonProvider(string connectionString, TelemetryClient telemetryClient)
        {
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Function that will return all of the configurations that are registered in Azure Table storage.
        /// </summary>
        /// <returns>A list of the configurations established.</returns>
        public async Task<List<PayCodeToTimeOffReasonsMappingEntity>> GetTimeOffReasonsAsync()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getConfigurationsProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getConfigurationsProps);

            // Table query
            var query = new TableQuery<PayCodeToTimeOffReasonsMappingEntity>();

            // Results list
            var results = new List<PayCodeToTimeOffReasonsMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                var queryResults = await this.timeOffReasonsCloudTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results;
        }

        /// <summary>
        /// Fetch reasons associated with a team.
        /// </summary>
        /// <param name="teamsId">teams id.</param>
        /// <param name="tenantId">tenant id of teams.</param>
        /// <returns>List of reasons.</returns>
        public async Task<Dictionary<string, string>> FetchReasonsForTeamsAsync(string teamsId, string tenantId)
        {
            try
            {
                this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
                await this.EnsureInitializedAsync().ConfigureAwait(false);
                var getTeamDepartmentMappingProps = new Dictionary<string, string>()
                {
                    { "QueryingTenantId", tenantId },
                    { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                };

                this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", getTeamDepartmentMappingProps);
                await this.EnsureInitializedAsync().ConfigureAwait(false);
                TableQuery<PayCodeToTimeOffReasonsMappingEntity> query = new TableQuery<PayCodeToTimeOffReasonsMappingEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, teamsId));

                TableContinuationToken token = null;

                var resultSegment = await this.timeOffReasonsCloudTable.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                token = resultSegment.ContinuationToken;
                return resultSegment.Results.Select(c => new KeyValuePair<string, string>(c.TimeOffReasonId, c.RowKey)).ToDictionary(c => c.Key, p => p.Value);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Get mapping for given reason.
        /// </summary>
        /// <param name="reason">The reason you are looking for.</param>
        /// <returns>The mapping for the reason.</returns>
        public async Task<PayCodeToTimeOffReasonsMappingEntity> FetchReasonAsync(string reason)
        {
            try
            {
                await this.EnsureInitializedAsync().ConfigureAwait(false);
                TableQuery<PayCodeToTimeOffReasonsMappingEntity> query =
                    new TableQuery<PayCodeToTimeOffReasonsMappingEntity>()
                        .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, reason));

                TableContinuationToken token = null;

                var resultSegment = await this.timeOffReasonsCloudTable.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                token = resultSegment.ContinuationToken;
                return resultSegment.Results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Delete all mappings except for ones with a given name.
        /// </summary>
        /// <param name="reasonsToRemove">The reasons name for the mappings you want to remove.</param>
        public async Task DeleteSpecificReasons(params string[] reasonsToRemove)
        {
            try
            {
                await this.EnsureInitializedAsync().ConfigureAwait(false);
                var query = new TableQuery<PayCodeToTimeOffReasonsMappingEntity>();

                TableContinuationToken token = null;
                var resultSegment = await this.timeOffReasonsCloudTable.ExecuteQuerySegmentedAsync(query, token).ConfigureAwait(false);
                token = resultSegment.ContinuationToken;

                // Create the batch operation.
                TableBatchOperation batchDeleteOperation = new TableBatchOperation();

                foreach (var reason in resultSegment.Results)
                {
                    if (reasonsToRemove.Contains(reason.RowKey))
                    {
                        this.telemetryClient.TrackTrace($"Adding {reason.RowKey} to delete operation.");
                        batchDeleteOperation.Delete(reason);
                    }
                }

                if (batchDeleteOperation.Count == 0)
                {
                    this.telemetryClient.TrackTrace("There are no time off reasons to be deleted.");
                    return;
                }

                // Execute the batch operation.
                await this.timeOffReasonsCloudTable.ExecuteBatchAsync(batchDeleteOperation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates the Azure table if the table does not already exist.
        /// </summary>
        /// <param name="connectionString">The connection string for the Azure storage.</param>
        /// <returns>A unit of execution.</returns>
        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace("Ensuring azure table for time off reasons exists.");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.timeOffReasonsCloudTable = cloudTableClient.GetTableReference(ConfigurationTableName);
            await this.timeOffReasonsCloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Having the necessary entities properly created.
        /// </summary>
        /// <returns>A unit of execution.</returns>
        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace("Ensuring necessary entities exist for time off reasons.");
            await this.initializeTask.Value.ConfigureAwait(false);
        }
    }
}