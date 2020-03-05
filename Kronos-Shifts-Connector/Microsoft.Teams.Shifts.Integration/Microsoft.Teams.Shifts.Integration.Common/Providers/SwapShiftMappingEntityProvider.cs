// <copyright file="SwapShiftMappingEntityProvider.cs" company="Microsoft">
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
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements methods that are defined in <see cref="ISwapShiftMappingEntityProvider"/>.
    /// </summary>
    public class SwapShiftMappingEntityProvider : ISwapShiftMappingEntityProvider
    {
        private const string SwapShiftTable = "SwapShiftMappingEntity";
        private const string ShiftTable = "ShiftEntityMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private CloudTable swapShiftEntityMappingTable;
        private CloudTable shiftEntityMappingTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapShiftMappingEntityProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        /// <param name="azureTableStorageHelper">Azure helper class.</param>
        public SwapShiftMappingEntityProvider(
            TelemetryClient telemetryClient,
            string connectionString,
            IAzureTableStorageHelper azureTableStorageHelper)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
            this.azureTableStorageHelper = azureTableStorageHelper;
        }

        /// <summary>
        /// Get all the time off mapping entities.
        /// </summary>
        /// <param name="kronosReqIds">Kronos req ids.</param>
        /// <returns>List of time Off mapping entities.</returns>
        public async Task<List<SwapShiftMappingEntity>> GetAllSwapShiftMappingEntitiesAsync(
            List<string> kronosReqIds)
        {
            if (kronosReqIds is null)
            {
                throw new ArgumentNullException(nameof(kronosReqIds));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<SwapShiftMappingEntity> query = new TableQuery<SwapShiftMappingEntity>();
            query.Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(
                        "KronosStatus",
                        QueryComparisons.Equal,
                        ApiConstants.Submitted), TableOperators.And,
                    TableQuery.GenerateFilterCondition(
                        "ShiftsStatus",
                        QueryComparisons.Equal,
                        ApiConstants.Pending)));

            // Results list
            List<SwapShiftMappingEntity> results = new List<SwapShiftMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.swapShiftEntityMappingTable.ExistsAsync().ConfigureAwait(false))
            {
                do
                {
                    var queryResults = await this.swapShiftEntityMappingTable.ExecuteQuerySegmentedAsync(
                        query,
                        continuationToken).ConfigureAwait(false);

                    continuationToken = queryResults.ContinuationToken;
                    results.AddRange(queryResults.Results);
                }
                while (continuationToken != null);
            }

            return results.Where(c => kronosReqIds.Contains(c.KronosReqId)).ToList();
        }

        /// <summary>
        /// Get all the time off mapping entities.
        /// </summary>
        /// <param name="swapShiftRedId">Teams Swap Shift Id.</param>
        /// <returns>Swap Shift Enity.</returns>
        public async Task<SwapShiftMappingEntity> GetKronosReqAsync(string swapShiftRedId)
        {
            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "SwapShiftRequestId", swapShiftRedId },
            };

            this.telemetryClient.TrackTrace($"{BusinessLogicResource.GetKronosReqAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", getEntitiesProps);

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            // Table query
            TableQuery<SwapShiftMappingEntity> query = new TableQuery<SwapShiftMappingEntity>();
            query.Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, swapShiftRedId));

            // Results list
            SwapShiftMappingEntity result = new SwapShiftMappingEntity();
            TableContinuationToken continuationToken = null;
            if (await this.swapShiftEntityMappingTable.ExistsAsync().ConfigureAwait(false))
            {
                    var queryResults = await this.swapShiftEntityMappingTable.ExecuteQuerySegmentedAsync(
                        query,
                        continuationToken).ConfigureAwait(false);

                    continuationToken = queryResults.ContinuationToken;
                    result = queryResults.Results?.FirstOrDefault();
            }

            this.telemetryClient.TrackTrace($"{BusinessLogicResource.GetKronosReqAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", getEntitiesProps);
            return result;
        }

        /// <summary>
        /// Method to check for the existence of a shift.
        /// </summary>
        /// <param name="shiftId">Teams Shift Id.</param>
        /// <returns>true if shift exists.</returns>
        public async Task<bool> CheckShiftExistanceAsync(string shiftId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<TeamsShiftMappingEntity> query = new TableQuery<TeamsShiftMappingEntity>();
            query.Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, shiftId));

            // Results list
            List<TeamsShiftMappingEntity> results = new List<TeamsShiftMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.swapShiftEntityMappingTable.ExistsAsync().ConfigureAwait(false))
            {
                do
                {
                    var queryResults = await this.shiftEntityMappingTable.ExecuteQuerySegmentedAsync(
                        query,
                        continuationToken).ConfigureAwait(false);

                    continuationToken = queryResults.ContinuationToken;
                    results.AddRange(queryResults.Results);
                }
                while (continuationToken != null);
            }

            if (results?.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method that will get the shift details.
        /// </summary>
        /// <param name="shiftId">The ID of the shift.</param>
        /// <returns>A unit of execution that contains a TeamsShiftMappingEntity.</returns>
        public async Task<TeamsShiftMappingEntity> GetShiftDetailsAsync(
            string shiftId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            string filter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, shiftId);

            // Table query
            var query = new TableQuery<TeamsShiftMappingEntity>().Where(filter);

            // Results list
            var results = new List<TeamsShiftMappingEntity>();
            TableContinuationToken continuationToken = null;

            do
            {
                var queryResults = await this.shiftEntityMappingTable.ExecuteQuerySegmentedAsync(
                    query,
                    continuationToken).ConfigureAwait(false);

                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            }
            while (continuationToken != null);

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Adds an entity to SwapShiftMapping table.
        /// </summary>
        /// <param name="swapShiftMappingEntity">SwapShiftMappingEntity instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddOrUpdateSwapShiftMappingAsync(
            SwapShiftMappingEntity swapShiftMappingEntity)
        {
            try
            {
                if (swapShiftMappingEntity is null)
                {
                    throw new ArgumentNullException(nameof(swapShiftMappingEntity));
                }

                await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(swapShiftMappingEntity, SwapShiftTable).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get all the pending request entities.
        /// </summary>
        /// <returns>List of Swap Shift Enity.</returns>
        public async Task<List<SwapShiftMappingEntity>> GetPendingRequest()
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<SwapShiftMappingEntity> query = new TableQuery<SwapShiftMappingEntity>();
            query.Where(TableQuery.CombineFilters(
                  TableQuery.GenerateFilterCondition(
                      "KronosStatus",
                      QueryComparisons.Equal,
                      givenValue: ApiConstants.Submitted),
                  TableOperators.And,
                  TableQuery.GenerateFilterCondition("ShiftsStatus", QueryComparisons.Equal, ApiConstants.Pending)));

            // Results list of pending request.
            List<SwapShiftMappingEntity> pendingRequests = new List<SwapShiftMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.swapShiftEntityMappingTable.ExistsAsync().ConfigureAwait(false))
            {
                do
                {
                    TableQuerySegment<SwapShiftMappingEntity> queryResults = await this.swapShiftEntityMappingTable.ExecuteQuerySegmentedAsync(
                    query,
                    continuationToken).ConfigureAwait(false);

                    continuationToken = queryResults.ContinuationToken;
                    pendingRequests.AddRange(queryResults.Results);
                }
                while (continuationToken != null);
            }

            return pendingRequests;
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{BusinessLogicResource.EnsureInitializedAsync}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{BusinessLogicResource.InitializeAsync}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.swapShiftEntityMappingTable = cloudTableClient.GetTableReference(SwapShiftTable);
            this.shiftEntityMappingTable = cloudTableClient.GetTableReference(ShiftTable);
            await this.swapShiftEntityMappingTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}