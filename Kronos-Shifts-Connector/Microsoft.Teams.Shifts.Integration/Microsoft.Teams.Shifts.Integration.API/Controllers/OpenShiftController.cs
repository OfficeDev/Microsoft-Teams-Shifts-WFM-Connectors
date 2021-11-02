// <copyright file="OpenShiftController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShifts;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using OpenShiftBatch = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.Batch;
    using SetupDetails = Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.SetupDetails;

    /// <summary>
    /// Open Shift controller.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/OpenShifts")]
    public class OpenShiftController : ControllerBase
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IOpenShiftActivity openShiftActivity;
        private readonly Utility utility;
        private readonly IGraphUtility graphUtility;
        private readonly IOpenShiftMappingEntityProvider openShiftMappingEntityProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenShiftController"/> class.
        /// </summary>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="openShiftActivity">OpenShift activity.</param>
        /// <param name="utility">Unique ID utility DI.</param>
        /// <param name="openShiftMappingEntityProvider">Open Shift Entity Mapping DI.</param>
        /// <param name="teamDepartmentMappingProvider">Team Department Mapping Provider DI.</param>
        /// <param name="httpClientFactory">http client.</param>
        /// <param name="openShiftRequestMappingEntityProvider">Open Shift Request Entity Mapping DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public OpenShiftController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IOpenShiftActivity openShiftActivity,
            Utility utility,
            IGraphUtility graphUtility,
            IOpenShiftMappingEntityProvider openShiftMappingEntityProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IHttpClientFactory httpClientFactory,
            IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider,
            BackgroundTaskWrapper taskWrapper)
        {
            if (appSettings is null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }

            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.openShiftActivity = openShiftActivity;
            this.utility = utility;
            this.graphUtility = graphUtility;
            this.openShiftMappingEntityProvider = openShiftMappingEntityProvider;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.httpClientFactory = httpClientFactory;
            this.openShiftRequestMappingEntityProvider = openShiftRequestMappingEntityProvider;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Creates an open shift in Kronos.
        /// </summary>
        /// <param name="openShift">The open shift entity to create in Kronos.</param>
        /// <param name="team">The team the open shift belongs to.</param>
        /// <returns>A response to return to teams.</returns>
        public async Task<ShiftsIntegResponse> CreateOpenShiftInKronosAsync(Models.IntegrationAPI.OpenShiftIS openShift, TeamToDepartmentJobMappingEntity team)
        {
            // The connector does not support drafting entities as it is not possible to draft shifts in Kronos.
            // Likewise there is no share schedule WFI call.
            if (openShift.DraftOpenShift != null)
            {
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "Creating an open shift as a draft is not supported for your team in Teams. Please publish changes directly using the 'Share' button.");
            }

            if (openShift.SharedOpenShift == null)
            {
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "An unexpected error occured. Could not create open shift.");
            }

            if (openShift.SharedOpenShift.Activities.Any())
            {
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "Adding activities to open shifts is not supported for your team in Teams. Remove all activities and try sharing again.");
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if ((allRequiredConfigurations?.IsAllSetUpExists).ErrorIfNull(openShift.Id, "App configuration incorrect.", out var response))
            {
                return response;
            }

            var possibleTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsBySchedulingGroupAsync(team.TeamId, openShift.SchedulingGroupId).ConfigureAwait(false);
            var openShiftOrgJobPath = possibleTeams.FirstOrDefault().RowKey;

            var commentTimeStamp = this.utility.UTCToKronosTimeZone(DateTime.UtcNow, team.KronosTimeZone).ToString(CultureInfo.InvariantCulture);
            var comments = XmlHelper.GenerateKronosComments(openShift.SharedOpenShift.Notes, this.appSettings.ShiftNotesCommentText, commentTimeStamp);

            var openShiftDetails = new
            {
                KronosStartDateTime = this.utility.UTCToKronosTimeZone(openShift.SharedOpenShift.StartDateTime, team.KronosTimeZone),
                KronosEndDateTime = this.utility.UTCToKronosTimeZone(openShift.SharedOpenShift.EndDateTime, team.KronosTimeZone),
                DisplayName = openShift.SharedOpenShift.DisplayName,
            };

            var creationResponse = await this.openShiftActivity.CreateOpenShiftAsync(
                new Uri(allRequiredConfigurations.WfmEndPoint),
                allRequiredConfigurations.KronosSession,
                this.utility.FormatDateForKronos(openShiftDetails.KronosStartDateTime),
                this.utility.FormatDateForKronos(openShiftDetails.KronosEndDateTime),
                openShiftDetails.KronosEndDateTime.Day > openShiftDetails.KronosStartDateTime.Day,
                Utility.OrgJobPathKronosConversion(openShiftOrgJobPath),
                openShiftDetails.DisplayName,
                openShiftDetails.KronosStartDateTime.TimeOfDay.ToString(),
                openShiftDetails.KronosEndDateTime.TimeOfDay.ToString(),
                openShift.SharedOpenShift.OpenSlotCount,
                comments).ConfigureAwait(false);

            if (creationResponse.Status != ApiConstants.Success)
            {
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "Open shift was not created successfully in Kronos.");
            }

            var monthPartitionKey = Utility.GetMonthPartition(
                this.utility.FormatDateForKronos(openShiftDetails.KronosStartDateTime),
                this.utility.FormatDateForKronos(openShiftDetails.KronosEndDateTime));

            await this.CreateAndStoreOpenShiftMapping(openShift, team, monthPartitionKey.FirstOrDefault(), openShiftOrgJobPath).ConfigureAwait(false);

            return ResponseHelper.CreateSuccessfulResponse(openShift.Id);
        }

        /// <summary>
        /// Get the list of open shift entities from Kronos and pushes to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">Checks if request is coming from logic app or portal.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task ProcessOpenShiftsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsAsync} started at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} for isRequestFromLogicApp:  {isRequestFromLogicApp}");

            // Adding the telemetry properties.
            var telemetryProps = new Dictionary<string, string>()
                {
                    { "MethodName", Resource.ProcessOpenShiftsAsync },
                    { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                };

            if (isRequestFromLogicApp == null)
            {
                throw new ArgumentNullException(nameof(isRequestFromLogicApp));
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if (allRequiredConfigurations?.IsAllSetUpExists == false)
            {
                throw new Exception($"{Resource.SyncOpenShiftsFromKronos} - {Resource.SetUpNotDoneMessage} - Some configuration settings were missing.");
            }

            this.utility.SetQuerySpan(Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture), out var openShiftStartDate, out var openShiftEndDate);

            // Check whether date range are in correct format.
            if (!Utility.CheckDates(openShiftStartDate, openShiftEndDate))
            {
                throw new Exception($"{Resource.SyncOpenShiftsFromKronos} - Query date was invalid.");
            }

            var monthPartitions = Utility.GetMonthPartition(openShiftStartDate, openShiftEndDate);

            if (monthPartitions?.Count > 0)
            {
                telemetryProps.Add("MonthPartitionsStatus", "There are no month partitions found!");
            }

            var orgJobBatchSize = int.Parse(this.appSettings.ProcessNumberOfOrgJobsInBatch, CultureInfo.InvariantCulture);
            var orgJobPaths = await this.teamDepartmentMappingProvider.GetAllOrgJobPathsAsync().ConfigureAwait(false);
            var mappedTeams = await this.teamDepartmentMappingProvider.GetMappedTeamToDeptsWithJobPathsAsync().ConfigureAwait(false);
            int orgJobPathIterations = Utility.GetIterablesCount(orgJobBatchSize, orgJobPaths.Count);

            // The monthPartitions is a list of strings which are formatted as: MM_YYYY to allow processing in batches
            foreach (var monthPartitionKey in monthPartitions)
            {
                if (monthPartitionKey == null)
                {
                    this.telemetryClient.TrackTrace($"{Resource.MonthPartitionKeyStatus} - MonthPartitionKey cannot be found please check the data.");
                    this.telemetryClient.TrackTrace(Resource.SyncOpenShiftsFromKronos, telemetryProps);
                    continue;
                }

                this.telemetryClient.TrackTrace($"Processing data for the month partition: {monthPartitionKey} at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

                Utility.GetNextDateSpan(
                    monthPartitionKey,
                    monthPartitions.FirstOrDefault(),
                    monthPartitions.LastOrDefault(),
                    openShiftStartDate,
                    openShiftEndDate,
                    out string queryStartDate,
                    out string queryEndDate);

                var orgJobPathList = new List<string>(orgJobPaths);

                // This for loop will iterate over the batched Org Job Paths.
                for (int iteration = 0; iteration < orgJobPathIterations; iteration++)
                {
                    this.telemetryClient.TrackTrace($"OpenShiftController - Processing on iteration number: {iteration}");

                    var orgJobsInBatch = orgJobPathList?.Skip(orgJobBatchSize * iteration).Take(orgJobBatchSize);

                    // Get the response for a batch of org job paths.
                    var openShiftsResponse = await this.GetOpenShiftResultsByOrgJobPathInBatchAsync(
                        allRequiredConfigurations.WFIId,
                        allRequiredConfigurations.WfmEndPoint,
                        allRequiredConfigurations.KronosSession,
                        orgJobsInBatch.ToList(),
                        queryStartDate,
                        queryEndDate).ConfigureAwait(false);

                    if (openShiftsResponse != null)
                    {
                        foreach (var orgJob in orgJobsInBatch)
                        {
                            this.telemetryClient.TrackTrace($"OpenShiftController - Processing the org job path: {orgJob}");

                            // Open Shift models for Create/Update operation
                            var lookUpEntriesFoundList = new List<AllOpenShiftMappingEntity>();
                            var openShiftsFoundList = new List<OpenShiftRequestModel>();
                            var openShiftsNotFoundList = new List<OpenShiftRequestModel>();

                            var formattedOrgJob = Utility.OrgJobPathDBConversion(orgJob);
                            var mappedOrgJobEntity = mappedTeams?.FirstOrDefault(x => x.PartitionKey == allRequiredConfigurations.WFIId && x.RowKey == formattedOrgJob);

                            // Retrieve lookUpData for the open shift entity.
                            var lookUpData = await this.openShiftMappingEntityProvider.GetAllOpenShiftMappingEntitiesInBatch(
                                monthPartitionKey,
                                formattedOrgJob,
                                queryStartDate,
                                queryEndDate).ConfigureAwait(false);

                            if (lookUpData != null)
                            {
                                // This foreach loop will process the openShiftSchedule item(s) that belong to a specific Kronos Org Job Path.
                                foreach (var openShiftSchedule in openShiftsResponse?.Schedules.Where(x => x.OrgJobPath == orgJob))
                                {
                                    this.telemetryClient.TrackTrace($"OpenShiftController - Processing the Open Shift schedule for: {openShiftSchedule.OrgJobPath}, and date range: {openShiftSchedule.QueryDateSpan}");

                                    // Kronos api returns any open shifts that occur in the date span provided.
                                    // We want only the entities that started within the query date span.
                                    var openShifts = ControllerHelper.FilterEntitiesByQueryDateSpan(openShiftSchedule.ScheduleItems?.ScheduleShifts, queryStartDate, queryEndDate);

                                    if (mappedOrgJobEntity == null)
                                    {
                                        this.telemetryClient.TrackTrace($"{Resource.SyncOpenShiftsFromKronos} - There is no mappedTeam found with WFI ID: {allRequiredConfigurations.WFIId}");
                                        continue;
                                    }

                                    if (openShifts.Count == 0)
                                    {
                                        this.telemetryClient.TrackTrace($"OpenShiftCount - {openShifts.Count} for {openShiftSchedule.OrgJobPath}");
                                        continue;
                                    }

                                    this.telemetryClient.TrackTrace($"OpenShiftController - Processing Open Shifts for the mapped team: {mappedOrgJobEntity.ShiftsTeamName}");

                                    // This foreach builds the Open Shift object to push to Shifts via Graph API.
                                    foreach (var openShift in openShifts)
                                    {
                                        this.telemetryClient.TrackTrace($"OpenShiftController - Processing {openShift.StartDate} with {openShift.ShiftSegments.ShiftSegment.Count} segments.");

                                        var teamsOpenShiftEntity = this.GenerateTeamsOpenShiftEntity(openShift, mappedOrgJobEntity);

                                        if (lookUpData.Count == 0)
                                        {
                                            this.telemetryClient.TrackTrace($"OpenShiftController - Adding {teamsOpenShiftEntity.KronosUniqueId} to the openShiftsNotFoundList as the lookUpData count = 0");
                                            openShiftsNotFoundList.Add(teamsOpenShiftEntity);
                                        }
                                        else
                                        {
                                            var kronosUniqueIdExists = lookUpData.Where(c => c.KronosOpenShiftUniqueId == teamsOpenShiftEntity.KronosUniqueId);

                                            if ((kronosUniqueIdExists != default(List<AllOpenShiftMappingEntity>)) && kronosUniqueIdExists.Any())
                                            {
                                                this.telemetryClient.TrackTrace($"OpenShiftController - Adding {kronosUniqueIdExists.FirstOrDefault().KronosOpenShiftUniqueId} to the lookUpEntriesFoundList as there is data in the lookUpData list.");
                                                lookUpEntriesFoundList.AddRange(kronosUniqueIdExists);
                                                openShiftsFoundList.Add(teamsOpenShiftEntity);
                                            }
                                            else
                                            {
                                                this.telemetryClient.TrackTrace($"OpenShiftController - Adding {teamsOpenShiftEntity.KronosUniqueId} to the openShiftsNotFoundList.");
                                                openShiftsNotFoundList.Add(teamsOpenShiftEntity);
                                            }
                                        }
                                    }
                                }

                                // We now want to process open shifts that are identical to one or more other open shifts,
                                // this includes open shifts with a slot count in Teams as well as any open shift with a matching hash.
                                await this.ProcessIdenticalOpenShifts(allRequiredConfigurations, monthPartitionKey, openShiftsFoundList, lookUpEntriesFoundList, mappedOrgJobEntity, lookUpData).ConfigureAwait(false);

                                if (lookUpData.Except(lookUpEntriesFoundList).Any())
                                {
                                    this.telemetryClient.TrackTrace($"OpenShiftController - The lookUpEntriesFoundList has {lookUpEntriesFoundList.Count} items which could be deleted");
                                    await this.DeleteOrphanDataOpenShiftsEntityMappingAsync(allRequiredConfigurations, lookUpEntriesFoundList, lookUpData, mappedOrgJobEntity).ConfigureAwait(false);
                                }

                                if (openShiftsNotFoundList.Count > 0)
                                {
                                    this.telemetryClient.TrackTrace($"OpenShiftController - The openShiftsNotFoundList has {openShiftsNotFoundList.Count} items which could be added.");
                                    await this.CreateEntryOpenShiftsEntityMappingAsync(allRequiredConfigurations, openShiftsNotFoundList, lookUpEntriesFoundList, monthPartitionKey, mappedOrgJobEntity).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                this.telemetryClient.TrackTrace($"{Resource.SyncOpenShiftsFromKronos} - There is no lookup data present with the schedulingGroupId: " + mappedOrgJobEntity?.TeamsScheduleGroupId);
                                continue;
                            }
                        }
                    }
                }
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsAsync} ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} for isRequestFromLogicApp:  {isRequestFromLogicApp}");
        }

        /// <summary>
        /// This method finds any idnetical open shifts before performing logic specific to OS
        /// that occur more than once enabling open shift slot count support.
        /// </summary>
        /// <param name="allRequiredConfigurations">The required configuration details.</param>
        /// <param name="monthPartitionKey">The month partition key currently being synced.</param>
        /// <param name="openShiftsFoundList">The list of Teams open shift entities we found in cache.</param>
        /// <param name="lookUpEntriesFoundList">The list of mapping entities retrieved from Kronos and found in cache.</param>
        /// <param name="mappedOrgJobEntity">The team deatils.</param>
        /// <param name="lookUpData">All of the cache records retrieved for the query date span.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ProcessIdenticalOpenShifts(
            SetupDetails allRequiredConfigurations,
            string monthPartitionKey,
            List<OpenShiftRequestModel> openShiftsFoundList,
            List<AllOpenShiftMappingEntity> lookUpEntriesFoundList,
            TeamToDepartmentJobMappingEntity mappedOrgJobEntity,
            List<AllOpenShiftMappingEntity> lookUpData)
        {
            var identicalOpenShifts = new List<AllOpenShiftMappingEntity>();

            // Count up each individual open shift stored in our cache.
            var map = new Dictionary<string, int>();
            foreach (var openShiftMapping in lookUpData)
            {
                if (map.ContainsKey(openShiftMapping.KronosOpenShiftUniqueId))
                {
                    map[openShiftMapping.KronosOpenShiftUniqueId] += openShiftMapping.KronosSlots;
                }
                else
                {
                    map.Add(openShiftMapping.KronosOpenShiftUniqueId, openShiftMapping.KronosSlots);
                }
            }

            foreach (var item in map)
            {
                if (item.Value > 1)
                {
                    // Where there are more than one identical entity in cache so we require seperate processing.
                    identicalOpenShifts.AddRange(lookUpData.Where(x => x.KronosOpenShiftUniqueId == item.Key));
                }
            }

            // Get each unique hash from the list of open shifts that occur more than once in cache.
            var identicalOpenShiftHash = identicalOpenShifts.Select(x => x.KronosOpenShiftUniqueId).Distinct();

            foreach (var openShiftHash in identicalOpenShiftHash)
            {
                // Retrieve the open shifts to process from cache as well as what we have retrieved from Kronos using the hash.
                var openShiftsInCacheToProcess = identicalOpenShifts.Where(x => x.KronosOpenShiftUniqueId == openShiftHash).ToList();
                var kronosOpenShiftsToProcess = openShiftsFoundList.Where(x => x.KronosUniqueId == openShiftHash).ToList();

                // Calculate the difference in number of open shifts between Kronos and cache
                var cacheOpenShiftSlotCount = 0;
                openShiftsInCacheToProcess.ForEach(x => cacheOpenShiftSlotCount += x.KronosSlots);
                var numberOfOpenShiftsToRemove = cacheOpenShiftSlotCount - kronosOpenShiftsToProcess.Count;

                if (numberOfOpenShiftsToRemove > 0)
                {
                    // More open shifts in cache than found in Kronos
                    await this.RemoveAdditionalOpenShiftsFromTeamsAsync(allRequiredConfigurations, mappedOrgJobEntity, openShiftsInCacheToProcess, numberOfOpenShiftsToRemove).ConfigureAwait(false);
                    continue;
                }

                if (numberOfOpenShiftsToRemove < 0)
                {
                    // Less open shifts in cache than found in Kronos
                    var openShiftsToAdd = new List<OpenShiftRequestModel>();

                    for (int i = numberOfOpenShiftsToRemove; i < 0; i++)
                    {
                        openShiftsToAdd.Add(kronosOpenShiftsToProcess.First());
                    }

                    await this.CreateEntryOpenShiftsEntityMappingAsync(allRequiredConfigurations, openShiftsToAdd, lookUpEntriesFoundList, monthPartitionKey, mappedOrgJobEntity).ConfigureAwait(false);
                    continue;
                }

                // The number of open shifts in Kronos and Teams matches meaning no action is needed
                continue;
            }
        }

        /// <summary>
        /// This method will remove open shifts from Teams in the event we find more OS in cache
        /// than retrieved from Kronos.
        /// </summary>
        /// <param name="allRequiredConfigurations">The required configuration details.</param>
        /// <param name="mappedOrgJobEntity">The team deatils.</param>
        /// <param name="openShiftsToProcess">All of the matching entities in cache.</param>
        /// <param name="numberOfOpenShiftsToRemove">The number of matching entities we want to remove.</param>
        /// <returns>A unit of execution.</returns>
        private async Task RemoveAdditionalOpenShiftsFromTeamsAsync(
            SetupDetails allRequiredConfigurations,
            TeamToDepartmentJobMappingEntity mappedOrgJobEntity,
            List<AllOpenShiftMappingEntity> openShiftsToProcess,
            int numberOfOpenShiftsToRemove)
        {
            var totalSlotsInCache = 0;
            openShiftsToProcess.ForEach(x => totalSlotsInCache += x.KronosSlots);

            if (!openShiftsToProcess.Any() || totalSlotsInCache < numberOfOpenShiftsToRemove)
            {
                // This code should not ever be used in theory however it protects against an infinite loop.
                this.telemetryClient.TrackTrace($"Error when removing open shifts from Teams. We need to remove {numberOfOpenShiftsToRemove} slots from cache but there is only {totalSlotsInCache} remaining in cache.");
                return;
            }

            do
            {
                // Find the mapping entity with the most slots
                var mappingEntityToDecrement = openShiftsToProcess.OrderByDescending(x => x.KronosSlots).First();

                if (mappingEntityToDecrement.KronosSlots - numberOfOpenShiftsToRemove > 0)
                {
                    // More slots than what we need to remove so update slot count in Teams and update cache.
                    var teamsOpenShiftEntity = await this.GetOpenShiftFromTeams(allRequiredConfigurations, mappedOrgJobEntity, mappingEntityToDecrement).ConfigureAwait(false);

                    if (teamsOpenShiftEntity != null)
                    {
                        teamsOpenShiftEntity.SharedOpenShift.OpenSlotCount -= numberOfOpenShiftsToRemove;
                        var response = await this.UpdateOpenShiftInTeams(allRequiredConfigurations, teamsOpenShiftEntity, mappedOrgJobEntity).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            mappingEntityToDecrement.KronosSlots -= numberOfOpenShiftsToRemove;
                            await this.openShiftMappingEntityProvider.SaveOrUpdateOpenShiftMappingEntityAsync(mappingEntityToDecrement).ConfigureAwait(false);

                            numberOfOpenShiftsToRemove = 0;
                        }
                    }
                }
                else
                {
                    // We need to remove more so delete the entity in Teams and delete from cache
                    var response = await this.DeleteOpenShiftInTeams(allRequiredConfigurations, mappingEntityToDecrement, mappedOrgJobEntity).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        numberOfOpenShiftsToRemove -= mappingEntityToDecrement.KronosSlots;
                    }
                }

                // Remove the mapping entity so we can process the next entity if necessary.
                openShiftsToProcess.Remove(mappingEntityToDecrement);
            }
            while (numberOfOpenShiftsToRemove > 0);
        }

        /// <summary>
        /// Generate a Teams open shift entity.
        /// </summary>
        /// <param name="kronosOpenShift">the Kronos open shift object.</param>
        /// <param name="mappedOrgJobEntity">The team details.</param>
        /// <returns>An open shift request model.</returns>
        private OpenShiftRequestModel GenerateTeamsOpenShiftEntity(OpenShiftBatch.ScheduleShift kronosOpenShift, TeamToDepartmentJobMappingEntity mappedOrgJobEntity)
        {
            var openShiftActivity = new List<Activity>();

            // This foreach loop will build the OpenShift activities.
            foreach (var segment in kronosOpenShift.ShiftSegments.ShiftSegment)
            {
                openShiftActivity.Add(new Activity
                {
                    IsPaid = true,
                    StartDateTime = this.utility.CalculateStartDateTime(segment, mappedOrgJobEntity.KronosTimeZone),
                    EndDateTime = this.utility.CalculateEndDateTime(segment, mappedOrgJobEntity.KronosTimeZone),
                    Code = string.Empty,
                    DisplayName = segment.SegmentTypeName,
                });
            }

            var openShift = new OpenShiftRequestModel()
            {
                SchedulingGroupId = mappedOrgJobEntity.TeamsScheduleGroupId,
                SharedOpenShift = new OpenShiftItem
                {
                    DisplayName = string.Empty,
                    OpenSlotCount = Constants.ShiftsOpenSlotCount,
                    Notes = this.utility.GetOpenShiftNotes(kronosOpenShift),
                    StartDateTime = openShiftActivity.First().StartDateTime,
                    EndDateTime = openShiftActivity.Last().EndDateTime,
                    Theme = this.appSettings.OpenShiftTheme,
                    Activities = openShiftActivity,
                },
            };

            // Generates the uniqueId for the OpenShift.
            openShift.KronosUniqueId = this.utility.CreateUniqueId(openShift, mappedOrgJobEntity);
            return openShift;
        }

        /// <summary>
        /// Retrieve an open shift from Teams by Team open shift Id.
        /// </summary>
        /// <param name="allRequiredConfigurations">The required configuration details.</param>
        /// <param name="mappedOrgJobEntity">The team details.</param>
        /// <param name="mappingEntityToDecrement">The mapping entity conatianing the details of the OS we want to retrieve.</param>
        /// <returns>A Graph open shift object.</returns>
        private async Task<GraphOpenShift> GetOpenShiftFromTeams(SetupDetails allRequiredConfigurations, TeamToDepartmentJobMappingEntity mappedOrgJobEntity, AllOpenShiftMappingEntity mappingEntityToDecrement)
        {
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.GraphConfigurationDetails.ShiftsAccessToken);

            var requestUrl = $"teams/{mappedOrgJobEntity.TeamId}/schedule/openShifts/{mappingEntityToDecrement.RowKey}";

            var response = await this.graphUtility.SendHttpRequest(allRequiredConfigurations.GraphConfigurationDetails, httpClient, HttpMethod.Get, requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<GraphOpenShift>(responseContent);
            }
            else
            {
                this.telemetryClient.TrackTrace($"The open shift with id {mappingEntityToDecrement.RowKey} could not be found in Teams. ");
                return null;
            }
        }

        /// <summary>
        /// Method that creates the Open Shift Entity Mapping, posts to Graph, and saves the data in Azure
        /// table storage upon successful creation in Graph.
        /// </summary>
        /// <param name="allRequiredConfiguration">The required configuration details.</param>
        /// <param name="openShiftNotFound">The open shift to post to Graph.</param>
        /// <param name="monthPartitionKey">The monthwise partition key.</param>
        /// <param name="mappedTeam">The mapped team.</param>
        /// <returns>A unit of execution.</returns>
        private async Task CreateEntryOpenShiftsEntityMappingAsync(
            SetupDetails allRequiredConfiguration,
            List<OpenShiftRequestModel> openShiftNotFound,
            List<AllOpenShiftMappingEntity> lookUpEntriesFoundList,
            string monthPartitionKey,
            TeamToDepartmentJobMappingEntity mappedTeam)
        {
            this.telemetryClient.TrackTrace($"CreateEntryOpenShiftsEntityMappingAsync start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            // This foreach loop iterates over the OpenShifts which are to be added into Shifts UI.
            foreach (var item in openShiftNotFound)
            {
                this.telemetryClient.TrackTrace($"Processing the open shift entity with schedulingGroupId: {item.SchedulingGroupId}");

                // create entries from not found list
                var telemetryProps = new Dictionary<string, string>()
                {
                    { "SchedulingGroupId", item.SchedulingGroupId },
                };

                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfiguration.GraphConfigurationDetails.ShiftsAccessToken);
                httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfiguration.WFIId);

                var requestString = JsonConvert.SerializeObject(item);
                var requestUrl = $"teams/{mappedTeam.TeamId}/schedule/openShifts";

                var response = await this.graphUtility.SendHttpRequest(allRequiredConfiguration.GraphConfigurationDetails, httpClient, HttpMethod.Post, requestUrl, requestString).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var openShiftResponse = JsonConvert.DeserializeObject<Models.Response.OpenShifts.GraphOpenShift>(responseContent);
                    var openShiftMappingEntity = this.CreateNewOpenShiftMappingEntity(openShiftResponse, item.KronosUniqueId, monthPartitionKey, mappedTeam?.RowKey);

                    telemetryProps.Add("ResultCode", response.StatusCode.ToString());
                    telemetryProps.Add("TeamsOpenShiftId", openShiftResponse.Id);

                    this.telemetryClient.TrackTrace(Resource.CreateEntryOpenShiftsEntityMappingAsync, telemetryProps);
                    await this.openShiftMappingEntityProvider.SaveOrUpdateOpenShiftMappingEntityAsync(openShiftMappingEntity).ConfigureAwait(false);

                    // Add the entity to the found list to prevent later processes from deleting
                    // the newly added entity.
                    lookUpEntriesFoundList.Add(openShiftMappingEntity);
                }
                else
                {
                    var errorProps = new Dictionary<string, string>()
                        {
                            { "ResultCode", response.StatusCode.ToString() },
                            { "ResponseHeader", response.Headers.ToString() },
                            { "SchedulingGroupId", item.SchedulingGroupId },
                            { "MappedTeamId", mappedTeam?.TeamId },
                        };

                    // Have the log to capture the error.
                    this.telemetryClient.TrackTrace(Resource.CreateEntryOpenShiftsEntityMappingAsync, errorProps);
                }
            }

            this.telemetryClient.TrackTrace($"CreateEntryOpenShiftsEntityMappingAsync end at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Update an open shift entity in Teams.
        /// </summary>
        /// <param name="allRequiredConfiguration">The required configuration details.</param>
        /// <param name="openShift">The open shift entity to update.</param>
        /// <param name="mappedTeam">The team details.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<HttpResponseMessage> UpdateOpenShiftInTeams(SetupDetails allRequiredConfiguration, GraphOpenShift openShift, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfiguration.GraphConfigurationDetails.ShiftsAccessToken);
            httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfiguration.WFIId);

            var requestString = JsonConvert.SerializeObject(openShift);
            var requestUrl = $"teams/{mappedTeam.TeamId}/schedule/openShifts/{openShift.Id}";

            var response = await this.graphUtility.SendHttpRequest(allRequiredConfiguration.GraphConfigurationDetails, httpClient, HttpMethod.Put, requestUrl, requestString).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var successfulUpdateProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", openShift.Id },
                            };

                this.telemetryClient.TrackTrace("Open shift updated.", successfulUpdateProps);
            }
            else
            {
                var errorUpdateProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", openShift.Id },
                            };

                this.telemetryClient.TrackTrace("Open shift could not be updated.", errorUpdateProps);
            }

            return response;
        }

        /// <summary>
        /// Method that will delete an orphaned open shift entity.
        /// </summary>
        /// <param name="allRequiredConfiguration">The required configuration details.</param>
        /// <param name="lookUpDataFoundList">The found open shifts.</param>
        /// <param name="lookUpData">All of the look up (reference data).</param>
        /// <param name="mappedTeam">The list of mapped teams.</param>
        /// <returns>A unit of execution.</returns>
        private async Task DeleteOrphanDataOpenShiftsEntityMappingAsync(
            SetupDetails allRequiredConfiguration,
            List<AllOpenShiftMappingEntity> lookUpDataFoundList,
            List<AllOpenShiftMappingEntity> lookUpData,
            TeamToDepartmentJobMappingEntity mappedTeam)
        {
            // delete entries from orphan list
            var orphanList = lookUpData.Except(lookUpDataFoundList);
            this.telemetryClient.TrackTrace($"DeleteOrphanDataOpenShiftsEntityMappingAsync started at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            // This foreach loop iterates over items that are to be deleted from Shifts UI. In other
            // words, these are Open Shifts which have been deleted in Kronos WFC, and those deletions
            // are propagating to Shifts.
            foreach (var item in orphanList)
            {
                this.telemetryClient.TrackTrace($"OpenShiftController - Checking {item.RowKey} to see if there are any Open Shift Requests");
                var isInOpenShiftRequestMappingTable = await this.openShiftRequestMappingEntityProvider.CheckOpenShiftRequestExistance(item.RowKey).ConfigureAwait(false);
                if (!isInOpenShiftRequestMappingTable)
                {
                    this.telemetryClient.TrackTrace($"{item.RowKey} is not in the Open Shift Request mapping table - deletion can be done.");
                    await this.DeleteOpenShiftInTeams(allRequiredConfiguration, item, mappedTeam).ConfigureAwait(false);
                }
                else
                {
                    // Log that the open shift exists in another table and it should not be deleted.
                    this.telemetryClient.TrackTrace($"OpenShiftController - Open Shift ID: {item.RowKey} is being handled by another process.");
                }
            }

            this.telemetryClient.TrackTrace($"DeleteOrphanDataOpenShiftsEntityMappingAsync ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Delete an open shift entity from Teams.
        /// </summary>
        /// <param name="allRequiredConfiguration">The required configuration details.</param>
        /// <param name="openShiftMapping">The open shift entity to delete.</param>
        /// <param name="mappedTeam">The team details.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<HttpResponseMessage> DeleteOpenShiftInTeams(SetupDetails allRequiredConfiguration, AllOpenShiftMappingEntity openShiftMapping, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfiguration.GraphConfigurationDetails.ShiftsAccessToken);
            httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfiguration.WFIId);

            var requestUrl = $"teams/{mappedTeam.TeamId}/schedule/openShifts/{openShiftMapping.RowKey}";

            var response = await this.graphUtility.SendHttpRequest(allRequiredConfiguration.GraphConfigurationDetails, httpClient, HttpMethod.Delete, requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var successfulDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", openShiftMapping.RowKey },
                            };

                this.telemetryClient.TrackTrace(Resource.DeleteOrphanDataOpenShiftsEntityMappingAsync, successfulDeleteProps);

                await this.openShiftMappingEntityProvider.DeleteOrphanDataFromOpenShiftMappingAsync(openShiftMapping).ConfigureAwait(false);
            }
            else
            {
                var errorDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", openShiftMapping.RowKey },
                            };

                this.telemetryClient.TrackTrace(Resource.DeleteOrphanDataOpenShiftsEntityMappingAsync, errorDeleteProps);
            }

            return response;
        }

        /// <summary>
        /// Method that will get the open shifts in a batch manner.
        /// </summary>
        /// <param name="workforceIntegrationId">The Workforce Integration Id.</param>
        /// <param name="kronosEndpoint">The Kronos WFC API Endpoint.</param>
        /// <param name="jSession">The Kronos Jsession.</param>
        /// <param name="orgJobPathsList">The list of org job paths.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>A unit of execution that contains the Kronos response model.</returns>
        private async Task<OpenShiftBatch.Response> GetOpenShiftResultsByOrgJobPathInBatchAsync(
            string workforceIntegrationId,
            string kronosEndpoint,
            string jSession,
            List<string> orgJobPathsList,
            string startDate,
            string endDate)
        {
            this.telemetryClient.TrackTrace($"OpenShiftController - GetOpenShiftResultsByOrgJobPathInBatchAsync started at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            var telemetryProps = new Dictionary<string, string>()
            {
                { "WorkforceIntegrationId", workforceIntegrationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(Resource.GetOpenShiftResultsByOrgJobPathInBatchAsync, telemetryProps);

            var openShiftQueryDateSpan = $"{startDate}-{endDate}";

            var openShifts = await this.openShiftActivity.GetOpenShiftDetailsInBatchAsync(
                new Uri(kronosEndpoint),
                jSession,
                orgJobPathsList,
                openShiftQueryDateSpan).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"OpenShiftController - GetOpenShiftResultsByOrgJobPathInBatchAsync ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            return openShifts;
        }

        /// <summary>
        /// Creates and stores a open shift mapping entity.
        /// </summary>
        /// <param name="openShift">An open shift from Shifts.</param>
        /// <param name="mappedTeam">A team mapping entity.</param>
        /// <param name="monthPartitionKey">The partition key for the shift.</param>
        /// <param name="openShiftOrgJobPath">The org job path of the open shift.</param>
        /// <returns>A task.</returns>
        private async Task CreateAndStoreOpenShiftMapping(Models.IntegrationAPI.OpenShiftIS openShift, TeamToDepartmentJobMappingEntity mappedTeam, string monthPartitionKey, string openShiftOrgJobPath)
        {
            var kronosUniqueId = this.utility.CreateOpenShiftInTeamsUniqueId(openShift, mappedTeam.KronosTimeZone, openShiftOrgJobPath);

            var startDateTime = DateTime.SpecifyKind(openShift.SharedOpenShift.StartDateTime, DateTimeKind.Utc);

            AllOpenShiftMappingEntity openShiftMappingEntity = new AllOpenShiftMappingEntity
            {
                PartitionKey = monthPartitionKey,
                RowKey = openShift.Id,
                KronosOpenShiftUniqueId = kronosUniqueId,
                KronosSlots = openShift.SharedOpenShift.OpenSlotCount,
                OrgJobPath = openShiftOrgJobPath,
                OpenShiftStartDate = startDateTime,
            };

            await this.openShiftMappingEntityProvider.SaveOrUpdateOpenShiftMappingEntityAsync(openShiftMappingEntity).ConfigureAwait(false);
        }

        /// <summary>
        /// Method that will create a new Open Shift Entity Mapping that conforms to the newest schema.
        /// </summary>
        /// <param name="responseModel">The OpenShift response from MS Graph.</param>
        /// <param name="uniqueId">The Kronos Unique Id.</param>
        /// <param name="monthPartitionKey">The monthwise partition key.</param>
        /// <param name="orgJobPath">The Kronos Org Job Path.</param>
        /// <returns>The new AllOpenShiftMappingEntity which conforms to schema.</returns>
        private AllOpenShiftMappingEntity CreateNewOpenShiftMappingEntity(
            Models.Response.OpenShifts.GraphOpenShift responseModel,
            string uniqueId,
            string monthPartitionKey,
            string orgJobPath)
        {
            var createNewOpenShiftMappingEntityProps = new Dictionary<string, string>()
            {
                { "GraphOpenShiftId", responseModel.Id },
                { "GraphOpenShiftEtag", responseModel.ETag },
                { "KronosUniqueId", uniqueId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var startDateTime = DateTime.SpecifyKind(responseModel.SharedOpenShift.StartDateTime.DateTime, DateTimeKind.Utc);

            AllOpenShiftMappingEntity openShiftMappingEntity = new AllOpenShiftMappingEntity
            {
                PartitionKey = monthPartitionKey,
                RowKey = responseModel.Id,
                KronosOpenShiftUniqueId = uniqueId,
                KronosSlots = responseModel.SharedOpenShift.OpenSlotCount,
                OrgJobPath = orgJobPath,
                OpenShiftStartDate = startDateTime,
            };

            this.telemetryClient.TrackTrace(Resource.CreateNewOpenShiftMappingEntity, createNewOpenShiftMappingEntityProps);

            return openShiftMappingEntity;
        }
    }
}