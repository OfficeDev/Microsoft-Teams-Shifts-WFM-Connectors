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
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
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
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "Creating a draft open shift is not supported. Please publish changes directly using the 'Share' button.");
            }

            if (openShift.SharedOpenShift == null)
            {
                return ResponseHelper.CreateBadResponse(openShift.Id, error: "An unexpected error occured. Could not create open shift.");
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if ((allRequiredConfigurations?.IsAllSetUpExists).ErrorIfNull(openShift.Id, "App configuration incorrect.", out var response))
            {
                return response;
            }

            var possibleTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsBySchedulingGroupAsync(team.TeamId, openShift.SchedulingGroupId).ConfigureAwait(false);
            var openShiftOrgJobPath = possibleTeams.FirstOrDefault().RowKey;

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
                openShift.SharedOpenShift.OpenSlotCount).ConfigureAwait(false);

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

                                    if (openShifts.Count > 0)
                                    {
                                        if (mappedOrgJobEntity != null)
                                        {
                                            this.telemetryClient.TrackTrace($"OpenShiftController - Processing Open Shifts for the mapped team: {mappedOrgJobEntity.ShiftsTeamName}");

                                            // This foreach builds the Open Shift object to push to Shifts via Graph API.
                                            foreach (var openShift in openShifts)
                                            {
                                                this.telemetryClient.TrackTrace($"OpenShiftController - Processing {openShift.StartDate} with {openShift.ShiftSegments.ShiftSegment.Count} segments.");

                                                var openShiftActivity = new List<Activity>();

                                                // This foreach loop will build the OpenShift activities.
                                                foreach (var segment in openShift.ShiftSegments.ShiftSegment)
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

                                                var shift = new OpenShiftRequestModel()
                                                {
                                                    SchedulingGroupId = mappedOrgJobEntity.TeamsScheduleGroupId,
                                                    SharedOpenShift = new OpenShiftItem
                                                    {
                                                        DisplayName = string.Empty,
                                                        OpenSlotCount = Constants.ShiftsOpenSlotCount,
                                                        Notes = this.utility.GetOpenShiftNotes(openShift),
                                                        StartDateTime = openShiftActivity.First().StartDateTime,
                                                        EndDateTime = openShiftActivity.Last().EndDateTime,
                                                        Theme = this.appSettings.OpenShiftTheme,
                                                        Activities = openShiftActivity,
                                                    },
                                                };

                                                // Generates the uniqueId for the OpenShift.
                                                shift.KronosUniqueId = this.utility.CreateUniqueId(shift, mappedOrgJobEntity);

                                                // Logging the output of the KronosHash creation.
                                                this.telemetryClient.TrackTrace("OpenShiftController-KronosHash: " + shift.KronosUniqueId);

                                                if (lookUpData.Count == 0)
                                                {
                                                    this.telemetryClient.TrackTrace($"OpenShiftController - Adding {shift.KronosUniqueId} to the openShiftsNotFoundList as the lookUpData count = 0");
                                                    openShiftsNotFoundList.Add(shift);
                                                }
                                                else
                                                {
                                                    var kronosUniqueIdExists = lookUpData.Where(c => c.RowKey == shift.KronosUniqueId);

                                                    if ((kronosUniqueIdExists != default(List<AllOpenShiftMappingEntity>)) && kronosUniqueIdExists.Any())
                                                    {
                                                        this.telemetryClient.TrackTrace($"OpenShiftController - Adding {kronosUniqueIdExists.FirstOrDefault().RowKey} to the lookUpEntriesFoundList as there is data in the lookUpData list.");
                                                        lookUpEntriesFoundList.Add(kronosUniqueIdExists.FirstOrDefault());
                                                    }
                                                    else
                                                    {
                                                        this.telemetryClient.TrackTrace($"OpenShiftController - Adding {shift.KronosUniqueId} to the openShiftsNotFoundList.");
                                                        openShiftsNotFoundList.Add(shift);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.telemetryClient.TrackTrace($"{Resource.SyncOpenShiftsFromKronos} - There is no mappedTeam found with WFI ID: {allRequiredConfigurations.WFIId}");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        this.telemetryClient.TrackTrace($"ScheduleShiftCount - {openShifts.Count} for {openShiftSchedule.OrgJobPath}");
                                        continue;
                                    }
                                }

                                if (lookUpData.Except(lookUpEntriesFoundList).Any())
                                {
                                    this.telemetryClient.TrackTrace($"OpenShiftController - The lookUpEntriesFoundList has {lookUpEntriesFoundList.Count} items which could be deleted");
                                    await this.DeleteOrphanDataOpenShiftsEntityMappingAsync(allRequiredConfigurations, lookUpEntriesFoundList, lookUpData, mappedOrgJobEntity).ConfigureAwait(false);
                                }

                                if (openShiftsNotFoundList.Count > 0)
                                {
                                    this.telemetryClient.TrackTrace($"OpenShiftController - The openShiftsNotFoundList has {openShiftsNotFoundList.Count} items which could be added.");
                                    await this.CreateEntryOpenShiftsEntityMappingAsync(allRequiredConfigurations, openShiftsNotFoundList, monthPartitionKey, mappedOrgJobEntity).ConfigureAwait(false);
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

                var requestString = JsonConvert.SerializeObject(item);
                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfiguration.ShiftsAccessToken);
                httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfiguration.WFIId);

                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + mappedTeam.TeamId + "/schedule/openShifts")
                {
                    Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
                })
                {
                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var openShiftResponse = JsonConvert.DeserializeObject<Models.Response.OpenShifts.GraphOpenShift>(responseContent);
                        var openShiftMappingEntity = this.CreateNewOpenShiftMappingEntity(openShiftResponse, item.KronosUniqueId, monthPartitionKey, mappedTeam?.RowKey);

                        telemetryProps.Add("ResultCode", response.StatusCode.ToString());
                        telemetryProps.Add("TeamsOpenShiftId", openShiftResponse.Id);

                        this.telemetryClient.TrackTrace(Resource.CreateEntryOpenShiftsEntityMappingAsync, telemetryProps);
                        await this.openShiftMappingEntityProvider.SaveOrUpdateOpenShiftMappingEntityAsync(openShiftMappingEntity).ConfigureAwait(false);
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

                        // Have the log to capture the 403.
                        this.telemetryClient.TrackTrace(Resource.CreateEntryOpenShiftsEntityMappingAsync, errorProps);
                    }
                }
            }

            this.telemetryClient.TrackTrace($"CreateEntryOpenShiftsEntityMappingAsync end at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
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
                this.telemetryClient.TrackTrace($"OpenShiftController - Checking {item.TeamsOpenShiftId} to see if there are any Open Shift Requests");
                var isInOpenShiftRequestMappingTable = await this.openShiftRequestMappingEntityProvider.CheckOpenShiftRequestExistance(item.TeamsOpenShiftId).ConfigureAwait(false);
                if (!isInOpenShiftRequestMappingTable)
                {
                    this.telemetryClient.TrackTrace($"{item.TeamsOpenShiftId} is not in the Open Shift Request mapping table - deletion can be done.");
                    var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfiguration.ShiftsAccessToken);
                    httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfiguration.WFIId);

                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, "teams/" + mappedTeam.TeamId + "/schedule/openShifts/" + item.TeamsOpenShiftId))
                    {
                        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            var successfulDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", item.TeamsOpenShiftId },
                            };

                            this.telemetryClient.TrackTrace(Resource.DeleteOrphanDataOpenShiftsEntityMappingAsync, successfulDeleteProps);

                            await this.openShiftMappingEntityProvider.DeleteOrphanDataFromOpenShiftMappingAsync(item).ConfigureAwait(false);
                        }
                        else
                        {
                            var errorDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                                { "MappedTeamId", mappedTeam?.TeamId },
                                { "OpenShiftIdToDelete", item.TeamsOpenShiftId },
                            };

                            this.telemetryClient.TrackTrace(Resource.DeleteOrphanDataOpenShiftsEntityMappingAsync, errorDeleteProps);
                        }
                    }
                }
                else
                {
                    // Log that the open shift exists in another table and it should not be deleted.
                    this.telemetryClient.TrackTrace($"OpenShiftController - Open Shift ID: {item.TeamsOpenShiftId} is being handled by another process.");
                }
            }

            this.telemetryClient.TrackTrace($"DeleteOrphanDataOpenShiftsEntityMappingAsync ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
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
            var kronosUniqueId = this.utility.CreateUniqueId(openShift, mappedTeam.KronosTimeZone, openShiftOrgJobPath);

            var startDateTime = DateTime.SpecifyKind(openShift.SharedOpenShift.StartDateTime, DateTimeKind.Utc);

            AllOpenShiftMappingEntity openShiftMappingEntity = new AllOpenShiftMappingEntity
            {
                PartitionKey = monthPartitionKey,
                RowKey = kronosUniqueId,
                TeamsOpenShiftId = openShift.Id,
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
                RowKey = uniqueId,
                TeamsOpenShiftId = responseModel.Id,
                KronosSlots = responseModel.SharedOpenShift.OpenSlotCount,
                OrgJobPath = orgJobPath,
                OpenShiftStartDate = startDateTime,
            };

            this.telemetryClient.TrackTrace(Resource.CreateNewOpenShiftMappingEntity, createNewOpenShiftMappingEntityProps);

            return openShiftMappingEntity;
        }
    }
}