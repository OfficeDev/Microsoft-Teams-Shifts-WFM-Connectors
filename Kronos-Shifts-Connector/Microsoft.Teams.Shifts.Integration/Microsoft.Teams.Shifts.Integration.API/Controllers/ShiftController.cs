// <copyright file="ShiftController.cs" company="Microsoft">
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
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.Request;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using IntegrationApi = Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using ShiftsShift = Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Shift;
    using UpcomingShiftsResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts;

    /// <summary>
    /// Shift controller.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/Shifts")]
    public class ShiftController : ControllerBase
    {
        private readonly IUserMappingProvider userMappingProvider;
        private readonly IShiftsActivity shiftsActivity;
        private readonly TelemetryClient telemetryClient;
        private readonly Utility utility;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly AppSettings appSettings;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftController"/> class.
        /// </summary>
        /// <param name="userMappingProvider">user Mapping Provider.</param>
        /// <param name="upcomingShiftsActivity">upcoming Shifts Activity.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="utility">UniqueId Utility DI.</param>
        /// <param name="shiftEntityMappingProvider">ShiftEntityMapper DI.</param>
        /// <param name="appSettings">app settings.</param>
        /// <param name="teamDepartmentMappingProvider">Team department mapping provider.</param>
        /// <param name="httpClientFactory">The HTTP Client DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public ShiftController(
            IUserMappingProvider userMappingProvider,
            IShiftsActivity upcomingShiftsActivity,
            TelemetryClient telemetryClient,
            Utility utility,
            IShiftMappingEntityProvider shiftEntityMappingProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            BackgroundTaskWrapper taskWrapper)
        {
            this.userMappingProvider = userMappingProvider;
            this.shiftsActivity = upcomingShiftsActivity;
            this.telemetryClient = telemetryClient;
            this.utility = utility;
            this.shiftMappingEntityProvider = shiftEntityMappingProvider;
            this.appSettings = appSettings;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.httpClientFactory = httpClientFactory;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Creates a shift mapping entity to be stored in the table.
        /// </summary>
        /// <param name="shift">The shift received from Shifts.</param>
        /// <param name="uniqueId">The unique ID for the shift.</param>
        /// <param name="kronoUserId">The user id of the user in Kronos.</param>
        /// <returns>Returns a <see cref="TeamsShiftMappingEntity"/>.</returns>
        public TeamsShiftMappingEntity CreateNewShiftMappingEntity(
            Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Shift shift,
            string uniqueId,
            string kronoUserId)
        {
            var createNewShiftMappingEntityProps = new Dictionary<string, string>()
            {
                { "GraphShiftId", shift?.Id },
                { "KronosUniqueId", uniqueId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var startDateTime = DateTime.SpecifyKind((DateTime)(shift.DraftShift?.StartDateTime ?? shift.SharedShift?.StartDateTime), DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind((DateTime)(shift.DraftShift?.EndDateTime ?? shift.SharedShift?.EndDateTime), DateTimeKind.Utc);

            TeamsShiftMappingEntity shiftMappingEntity = new TeamsShiftMappingEntity
            {
                AadUserId = shift.UserId,
                KronosUniqueId = uniqueId,
                KronosPersonNumber = kronoUserId,
                ShiftStartDate = startDateTime,
                ShiftEndDate = endDateTime,
            };

            this.telemetryClient.TrackTrace("Creating new shift mapping entity.", createNewShiftMappingEntityProps);

            return shiftMappingEntity;
        }

        /// <summary>
        /// Gets the shifts for a given Kronos user id.
        /// </summary>
        /// <param name="kronosUserId">A Kronos user id.</param>
        /// <param name="queryStartDate">The query start date.</param>
        /// <param name="queryEndDate">The query end date.</param>
        /// <returns>The schedule response.</returns>
        public async Task<App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.Response> GetShiftsForUser(string kronosUserId, string queryStartDate, string queryEndDate)
        {
            App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.Response shiftsResponse = null;

            this.utility.SetQuerySpan(Convert.ToBoolean(false, CultureInfo.InvariantCulture), out string shiftStartDate, out string shiftEndDate);
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(shiftStartDate, shiftEndDate);
            if (!isCorrectDateRange)
            {
                throw new Exception($"{Resource.SyncShiftsFromKronos} - Query date was invalid.");
            }

            if ((bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                var user = new List<ResponseHyperFindResult>()
                        {
                            new ResponseHyperFindResult { PersonNumber = kronosUserId },
                        };

                // Get shift response for a batch of users.
                shiftsResponse = await this.shiftsActivity.ShowUpcomingShiftsInBatchAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        queryStartDate,
                        queryEndDate,
                        user).ConfigureAwait(false);
            }

            return shiftsResponse;
        }

        /// <summary>
        /// Deletes the shift from Kronos and the database.
        /// </summary>
        /// <param name="shift">The shift to remove.</param>
        /// <param name="user">The user the shift is for.</param>
        /// <param name="mappedTeam">The team the user is in.</param>
        /// <returns>A response for teams.</returns>
        public async Task<ShiftsIntegResponse> DeleteShiftInKronosAsync(ShiftsShift shift, AllUserMappingEntity user, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            // The connector does not support drafting entities as it is not possible to draft shifts in Kronos.
            // Likewise there is no share schedule WFI call.
            if (shift.DraftShift != null)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "Deleting a shift as a draft is not supported. Please publish changes directly using the 'Share' button.");
            }

            if (shift.SharedShift == null)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "An unexpected error occured. Could not delete the shift.");
            }

            if (user.ErrorIfNull(shift.Id, "User could not be found.", out var response))
            {
                return response;
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if ((allRequiredConfigurations?.IsAllSetUpExists == false).ErrorIfNull(shift.Id, "App configuration incorrect.", out response))
            {
                return response;
            }

            // Convert to Kronos local time.
            var kronosStartDateTime = this.utility.UTCToKronosTimeZone(shift.SharedShift.StartDateTime, mappedTeam.KronosTimeZone);
            var kronosEndDateTime = this.utility.UTCToKronosTimeZone(shift.SharedShift.EndDateTime, mappedTeam.KronosTimeZone);

            var deletionResponse = await this.shiftsActivity.DeleteShift(
                new Uri(allRequiredConfigurations.WfmEndPoint),
                allRequiredConfigurations.KronosSession,
                this.utility.FormatDateForKronos(kronosStartDateTime),
                this.utility.FormatDateForKronos(kronosEndDateTime),
                kronosEndDateTime.Day > kronosStartDateTime.Day,
                Utility.OrgJobPathKronosConversion(user.PartitionKey),
                user.RowKey,
                kronosStartDateTime.TimeOfDay.ToString(),
                kronosEndDateTime.TimeOfDay.ToString()).ConfigureAwait(false);

            if (deletionResponse.Status != Success)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "Shift was not successfully removed from Kronos.");
            }

            await this.DeleteShiftMapping(shift).ConfigureAwait(false);
            return ResponseHelper.CreateSuccessfulResponse(shift.Id);
        }

        /// <summary>
        /// Adds the shift to Kronos and the database.
        /// </summary>
        /// <param name="shift">The shift to add.</param>
        /// <param name="user">The user the shift is for.</param>
        /// <param name="mappedTeam">The team the user is in.</param>
        /// <returns>A response for teams.</returns>
        public async Task<ShiftsIntegResponse> CreateShiftInKronosAsync(ShiftsShift shift, AllUserMappingEntity user, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            // The connector does not support drafting entities as it is not possible to draft shifts in Kronos.
            // Likewise there is no share schedule WFI call.
            if (shift.DraftShift != null)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "Creating a draft shift is not supported. Please publish changes directly using the 'Share' button.");
            }

            if (shift.SharedShift == null)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "An unexpected error occured. Could not create the shift.");
            }

            if (user.ErrorIfNull(shift.Id, "User could not be found.", out var response))
            {
                return response;
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if ((allRequiredConfigurations?.IsAllSetUpExists).ErrorIfNull(shift.Id, "App configuration incorrect.", out response))
            {
                return response;
            }

            var kronosStartDateTime = this.utility.UTCToKronosTimeZone(shift.SharedShift.StartDateTime, mappedTeam.KronosTimeZone);
            var kronosEndDateTime = this.utility.UTCToKronosTimeZone(shift.SharedShift.EndDateTime, mappedTeam.KronosTimeZone);

            var creationResponse = await this.shiftsActivity.CreateShift(
                new Uri(allRequiredConfigurations.WfmEndPoint),
                allRequiredConfigurations.KronosSession,
                this.utility.FormatDateForKronos(kronosStartDateTime),
                this.utility.FormatDateForKronos(kronosEndDateTime),
                kronosEndDateTime.Day > kronosStartDateTime.Day,
                Utility.OrgJobPathKronosConversion(user.PartitionKey),
                user.RowKey,
                kronosStartDateTime.TimeOfDay.ToString(),
                kronosEndDateTime.TimeOfDay.ToString()).ConfigureAwait(false);

            if (creationResponse.Status != Success)
            {
                return ResponseHelper.CreateBadResponse(shift.Id, error: "Shift was not created successfully in Kronos.");
            }

            var monthPartitionKey = Utility.GetMonthPartition(this.utility.FormatDateForKronos(kronosStartDateTime), this.utility.FormatDateForKronos(kronosEndDateTime));

            await this.CreateAndStoreShiftMapping(shift, user, mappedTeam, monthPartitionKey).ConfigureAwait(false);

            return ResponseHelper.CreateSuccessfulResponse(shift.Id);
        }

        /// <summary>
        /// Edits a shift in Kronos and updates the database.
        /// </summary>
        /// <param name="editedShift">The shift to edit.</param>
        /// <param name="user">The user the shift is for.</param>
        /// <param name="mappedTeam">The team the user is in.</param>
        /// <returns>A response for teams.</returns>
        public async Task<ShiftsIntegResponse> EditShiftInKronosAsync(ShiftsShift editedShift, AllUserMappingEntity user, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            // The connector does not support drafting entities as it is not possible to draft shifts in Kronos.
            // Likewise there is no share schedule WFI call.
            if (editedShift.DraftShift != null)
            {
                return ResponseHelper.CreateBadResponse(editedShift.Id, error: "Editing a shift as a draft is not supported. Please publish changes directly using the 'Share' button.");
            }

            if (editedShift.SharedShift == null)
            {
                return ResponseHelper.CreateBadResponse(editedShift.Id, error: "An unexpected error occured. Could not edit the shift.");
            }

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            if ((allRequiredConfigurations?.IsAllSetUpExists).ErrorIfNull(editedShift.Id, "App configuration incorrect.", out var response))
            {
                return response;
            }

            // We need to get all other shifts the employee works that day.
            var kronosStartDateTime = this.utility.UTCToKronosTimeZone(editedShift.SharedShift.StartDateTime, mappedTeam.KronosTimeZone);
            var kronosEndDateTime = this.utility.UTCToKronosTimeZone(editedShift.SharedShift.EndDateTime, mappedTeam.KronosTimeZone);

            var monthPartitionKey = Utility.GetMonthPartition(this.utility.FormatDateForKronos(kronosStartDateTime), this.utility.FormatDateForKronos(kronosEndDateTime));

            var shiftToReplace = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(editedShift.Id).ConfigureAwait(false);
            var shiftToReplaceStartDateTime = this.utility.UTCToKronosTimeZone(shiftToReplace.ShiftStartDate, mappedTeam.KronosTimeZone);
            var shiftToReplaceEndDateTime = this.utility.UTCToKronosTimeZone(shiftToReplace.ShiftEndDate, mappedTeam.KronosTimeZone);

            var editResponse = await this.shiftsActivity.EditShift(
                new Uri(allRequiredConfigurations.WfmEndPoint),
                allRequiredConfigurations.KronosSession,
                this.utility.FormatDateForKronos(kronosStartDateTime),
                this.utility.FormatDateForKronos(kronosEndDateTime),
                kronosEndDateTime.Day > kronosStartDateTime.Day,
                Utility.OrgJobPathKronosConversion(user.PartitionKey),
                user.RowKey,
                kronosStartDateTime.TimeOfDay.ToString(),
                kronosEndDateTime.TimeOfDay.ToString(),
                this.utility.FormatDateForKronos(shiftToReplaceStartDateTime),
                this.utility.FormatDateForKronos(shiftToReplaceEndDateTime),
                shiftToReplaceStartDateTime.TimeOfDay.ToString(),
                shiftToReplaceEndDateTime.TimeOfDay.ToString()).ConfigureAwait(false);

            if (editResponse.Status != Success)
            {
                return ResponseHelper.CreateBadResponse(editedShift.Id, error: "Shift could not be edited in Kronos.");
            }

            await this.DeleteShiftMapping(editedShift).ConfigureAwait(false);
            await this.CreateAndStoreShiftMapping(editedShift, user, mappedTeam, monthPartitionKey).ConfigureAwait(false);

            return ResponseHelper.CreateSuccessfulResponse(editedShift.Id);
        }

        /// <summary>
        /// Removes a shift mapping entity.
        /// </summary>
        /// <param name="shift">A shift from Shifts.</param>
        /// <returns>A task.</returns>
        private async Task DeleteShiftMapping(ShiftsShift shift)
        {
            var shiftMappingEntity = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(shift.Id).ConfigureAwait(false);
            await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(shiftMappingEntity).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and stores a shift mapping entity.
        /// </summary>
        /// <param name="shift">A shift from Shifts.</param>
        /// <param name="user">A user mapping entity.</param>
        /// <param name="mappedTeam">A team mapping entity.</param>
        /// <param name="monthPartitionKey">The partition key for the shift.</param>
        /// <returns>A task.</returns>
        private async Task CreateAndStoreShiftMapping(ShiftsShift shift, AllUserMappingEntity user, TeamToDepartmentJobMappingEntity mappedTeam, List<string> monthPartitionKey)
        {
            var kronosUniqueId = this.utility.CreateUniqueId(shift, mappedTeam.KronosTimeZone);
            var shiftMappingEntity = this.CreateNewShiftMappingEntity(shift, kronosUniqueId, user.RowKey);
            await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(shiftMappingEntity, shift.Id, monthPartitionKey[0]).ConfigureAwait(false);
        }

        /// <summary>
        /// Start shifts sync from Kronos to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">Checks if request is coming from logic app or portal.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task ProcessShiftsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessShiftsAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} for isRequestFromLogicApp: {isRequestFromLogicApp}");

            this.utility.SetQuerySpan(Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture), out string shiftStartDate, out string shiftEndDate);
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(shiftStartDate, shiftEndDate);

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                // Get the mapped user details from user to user mapping table.
                var kronosUsers = await this.GetAllMappedUserDetailsAsync(allRequiredConfigurations.WFIId).ConfigureAwait(false);
                if (kronosUsers.Any())
                {
                    var monthPartitions = Utility.GetMonthPartition(shiftStartDate, shiftEndDate);

                    if (monthPartitions.Count > 0)
                    {
                        var processNumberOfUsersInBatch = this.appSettings.ProcessNumberOfUsersInBatch;
                        var userCount = kronosUsers.Count();
                        int userIteration = Utility.GetIterablesCount(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture), userCount);

                        foreach (var monthPartitionKey in monthPartitions)
                        {
                            string queryStartDate, queryEndDate;
                            Utility.GetNextDateSpan(
                                monthPartitionKey,
                                monthPartitions.FirstOrDefault(),
                                monthPartitions.LastOrDefault(),
                                shiftStartDate,
                                shiftEndDate,
                                out queryStartDate,
                                out queryEndDate);

                            var processUsersInBatchList = new List<App.KronosWfc.Models.ResponseEntities.HyperFind.ResponseHyperFindResult>();
                            foreach (var item in kronosUsers?.ToList())
                            {
                                processUsersInBatchList.Add(new App.KronosWfc.Models.ResponseEntities.HyperFind.ResponseHyperFindResult
                                {
                                    PersonNumber = item.KronosPersonNumber,
                                });
                            }

                            var processBatchUsersQueue = new Queue<App.KronosWfc.Models.ResponseEntities.HyperFind.ResponseHyperFindResult>(processUsersInBatchList);
                            var processKronosUsersQueue = new Queue<UserDetailsModel>(kronosUsers);

                            for (int batchedUserCount = 0; batchedUserCount < userIteration; batchedUserCount++)
                            {
                                var processKronosUsersQueueInBatch = processKronosUsersQueue?.Skip(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture) * batchedUserCount).Take(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture));
                                var processBatchUsersQueueInBatch = processBatchUsersQueue?.Skip(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture) * batchedUserCount).Take(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture));

                                var lookUpData = await this.shiftMappingEntityProvider.GetAllShiftMappingEntitiesInBatchAsync(
                                    processKronosUsersQueueInBatch,
                                    monthPartitionKey,
                                    queryStartDate,
                                    queryEndDate).ConfigureAwait(false);

                                // Get shift response for a batch of users.
                                var shiftsResponse = await this.shiftsActivity.ShowUpcomingShiftsInBatchAsync(
                                        new Uri(allRequiredConfigurations.WfmEndPoint),
                                        allRequiredConfigurations.KronosSession,
                                        DateTime.Now.ToString(queryStartDate, CultureInfo.InvariantCulture),
                                        DateTime.Now.ToString(queryEndDate, CultureInfo.InvariantCulture),
                                        processBatchUsersQueueInBatch.ToList()).ConfigureAwait(false);

                                // Kronos api returns any shifts that occur in the date span provided.
                                // We want only the entities that started within the query date span.
                                var shifts = ControllerHelper.FilterEntitiesByQueryDateSpan(shiftsResponse?.Schedule?.ScheduleItems?.ScheduleShifts, queryStartDate, queryEndDate);

                                var lookUpEntriesFoundList = new List<TeamsShiftMappingEntity>();
                                var shiftsNotFoundList = new List<Shift>();

                                var userModelList = new List<UserDetailsModel>();
                                var userModelNotFoundList = new List<UserDetailsModel>();

                                await this.ProcessShiftEntitiesBatchAsync(
                                    allRequiredConfigurations,
                                    lookUpEntriesFoundList,
                                    shiftsNotFoundList,
                                    userModelList,
                                    userModelNotFoundList,
                                    lookUpData,
                                    processKronosUsersQueueInBatch,
                                    shifts,
                                    monthPartitionKey).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            else
            {
                this.telemetryClient.TrackTrace("SyncShiftsFromKronos - " + Resource.SetUpNotDoneMessage);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessShiftsAsync} completed at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);
        }

        /// <summary>
        /// Method to process the shift entities in a batch manner.
        /// </summary>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="lookUpEntriesFoundList">The lookUp entries that have been found.</param>
        /// <param name="shiftsNotFoundList">The shifts that have not been found.</param>
        /// <param name="userModelList">The users list.</param>
        /// <param name="userModelNotFoundList">The list of users that have not been found.</param>
        /// <param name="lookUpData">The look up data from the Shift Entity Mapping table.</param>
        /// <param name="processKronosUsersQueueInBatch">The Kronos users in the queue.</param>
        /// <param name="shifts">The Shifts Response from MS Graph.</param>
        /// <param name="monthPartitionKey">The monthwise partition.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ProcessShiftEntitiesBatchAsync(
            IntegrationApi.SetupDetails configurationDetails,
            List<TeamsShiftMappingEntity> lookUpEntriesFoundList,
            List<Shift> shiftsNotFoundList,
            List<UserDetailsModel> userModelList,
            List<UserDetailsModel> userModelNotFoundList,
            List<TeamsShiftMappingEntity> lookUpData,
            IEnumerable<UserDetailsModel> processKronosUsersQueueInBatch,
            List<UpcomingShiftsResponse.ScheduleShift> shifts,
            string monthPartitionKey)
        {
            this.telemetryClient.TrackTrace($"ShiftController - ProcessShiftEntitiesBatchAsync started at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            var shiftNotes = string.Empty;

            // This foreach loop processes each user in the batch.
            foreach (var user in processKronosUsersQueueInBatch)
            {
                // This foreach loop will process the shift(s) that belong to each user.
                foreach (var kronosShift in shifts)
                {
                    if (user.KronosPersonNumber == kronosShift.Employee.FirstOrDefault().PersonNumber)
                    {
                        this.telemetryClient.TrackTrace($"ShiftController - Processing the shifts for user: {user.KronosPersonNumber}");

                        var shift = this.GenerateTeamsShiftObject(user, kronosShift);

                        shift.KronosUniqueId = this.utility.CreateUniqueId(shift, user.KronosTimeZone);

                        this.telemetryClient.TrackTrace($"ShiftController-KronosHash: {shift.KronosUniqueId}");

                        userModelList.Add(user);

                        if (lookUpData.Count == 0)
                        {
                            shiftsNotFoundList.Add(shift);
                            userModelNotFoundList.Add(user);
                        }
                        else
                        {
                            var kronosUniqueIdExists = lookUpData.Where(c => c.KronosUniqueId == shift.KronosUniqueId);

                            if (kronosUniqueIdExists.Any() && (kronosUniqueIdExists != default(List<TeamsShiftMappingEntity>)))
                            {
                                lookUpEntriesFoundList.Add(kronosUniqueIdExists.FirstOrDefault());
                            }
                            else
                            {
                                shiftsNotFoundList.Add(shift);
                                userModelNotFoundList.Add(user);
                            }
                        }
                    }
                }
            }

            if (lookUpData.Except(lookUpEntriesFoundList).Any())
            {
                await this.DeleteOrphanDataShiftsEntityMappingAsync(configurationDetails, lookUpEntriesFoundList, userModelList, lookUpData).ConfigureAwait(false);
            }

            await this.CreateEntryShiftsEntityMappingAsync(configurationDetails, userModelNotFoundList, shiftsNotFoundList, monthPartitionKey).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"ShiftController - ProcessShiftEntitiesBatchAsync ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Creates a Teams shift object for each shift.
        /// </summary>
        /// <param name="user">the user details the shift is assigned to.</param>
        /// <param name="kronosShift">The shift to create a Teams shift from.</param>
        /// <returns>A Teams shift object.</returns>
        private Shift GenerateTeamsShiftObject(UserDetailsModel user, UpcomingShiftsResponse.ScheduleShift kronosShift)
        {
            List<ShiftActivity> shiftActivity = new List<ShiftActivity>();
            var shiftDisplayName = string.Empty;

            foreach (var activity in kronosShift?.ShiftSegments)
            {
                var activityDisplayName = string.Empty;

                // If there is an orgJobPath in the shift segment then this segment is working
                // a job other than the employees main job (shift transfer)
                if (activity.OrgJobPath != null)
                {
                    var splitOrgJobPath = activity.OrgJobPath.Split("/");

                    // Number of elements to take from the split org job path array.
                    var numberOfOrgJobPathSections = int.Parse(this.appSettings.NumberOfOrgJobPathSectionsForActivityName, CultureInfo.InvariantCulture);

                    // Ensure that the max number of sections is less than or equal to total number of sections.
                    numberOfOrgJobPathSections = numberOfOrgJobPathSections <= splitOrgJobPath.Length ? numberOfOrgJobPathSections : splitOrgJobPath.Length;

                    var orgJobPathSections = new List<string>(numberOfOrgJobPathSections);

                    for (int i = splitOrgJobPath.Length - numberOfOrgJobPathSections; i < splitOrgJobPath.Length; i++)
                    {
                        orgJobPathSections.Add(splitOrgJobPath[i]);
                    }

                    activityDisplayName = string.Join("-", orgJobPathSections);

                    // Teams UI has a character limit of 50 so if we exceed this we want to trim the string down
                    // and prepend an elipses to indicate we have trimmed.
                    if (activityDisplayName.Length > 50)
                    {
                        var trimmedDisplayName = activityDisplayName.Substring(activityDisplayName.Length - 47);
                        activityDisplayName = trimmedDisplayName.Insert(0, "...");
                    }
                }

                shiftActivity.Add(new ShiftActivity
                {
                    IsPaid = true,
                    StartDateTime = this.utility.CalculateStartDateTime(activity, user.KronosTimeZone),
                    EndDateTime = this.utility.CalculateEndDateTime(activity, user.KronosTimeZone),
                    Code = string.Empty,
                    DisplayName = activity.OrgJobPath != null ? activityDisplayName : activity.SegmentTypeName,
                });
            }

            var displayNameStartTime = DateTime.Parse(kronosShift.ShiftSegments.First().StartTime, CultureInfo.InvariantCulture);
            var displayNameEndTime = DateTime.Parse(kronosShift.ShiftSegments.Last().EndTime, CultureInfo.InvariantCulture);

            // We want to make it clear in the shift label/display name that this is a transferred shift.
            if (kronosShift.ShiftSegments.Any(x => x.SegmentTypeName == "TRANSFER"))
            {
                var displayNameTime = $"{displayNameStartTime.ToString("HH:mm", CultureInfo.InvariantCulture)} - {displayNameEndTime.ToString("HH:mm", CultureInfo.InvariantCulture)}";
                shiftDisplayName = $"TRANSFER {displayNameTime}";
            }

            var shift = new Shift
            {
                UserId = user.ShiftUserId,
                SchedulingGroupId = user.ShiftScheduleGroupId,
                SharedShift = new SharedShift
                {
                    DisplayName = shiftDisplayName,
                    Notes = this.utility.GetShiftNotes(kronosShift),
                    StartDateTime = shiftActivity[0].StartDateTime,
                    EndDateTime = shiftActivity[shiftActivity.Count - 1].EndDateTime,
                    Theme = this.appSettings.ShiftTheme,
                    Activities = shiftActivity,
                },
            };

            return shift;
        }

        /// <summary>
        /// Method that will create the new Shifts Entity Mapping.
        /// </summary>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="userModelNotFoundList">The list of users that have not been found.</param>
        /// <param name="notFoundShifts">The shifts which have not been found.</param>
        /// <param name="monthPartitionKey">The monthwise partition key.</param>
        /// <returns>A unit of execution.</returns>
        private async Task CreateEntryShiftsEntityMappingAsync(
            IntegrationApi.SetupDetails configurationDetails,
            List<UserDetailsModel> userModelNotFoundList,
            List<Shift> notFoundShifts,
            string monthPartitionKey)
        {
            // create entries from not found list
            for (int i = 0; i < notFoundShifts.Count; i++)
            {
                var requestString = JsonConvert.SerializeObject(notFoundShifts[i]);

                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configurationDetails.ShiftsAccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", configurationDetails.WFIId);

                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + userModelNotFoundList[i].ShiftTeamId + "/schedule/shifts")
                {
                    Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
                })
                {
                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var shiftResponse = JsonConvert.DeserializeObject<Models.Response.Shifts.Shift>(responseContent);
                        var shiftId = shiftResponse.Id;
                        var shiftMappingEntity = this.CreateNewShiftMappingEntity(shiftResponse, notFoundShifts[i].KronosUniqueId, userModelNotFoundList[i]);
                        await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(shiftMappingEntity, shiftId, monthPartitionKey).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        var errorProps = new Dictionary<string, string>()
                        {
                            { "ResultCode", response.StatusCode.ToString() },
                            { "IntegerResult", Convert.ToInt32(response.StatusCode, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) },
                        };

                        // Have the log to capture the 403.
                        this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, errorProps);
                    }
                }
            }
        }

        /// <summary>
        /// Method to delete an orphan shift - happens when a shift is deleted from Kronos.
        /// </summary>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="lookUpDataFoundList">The list of data that has been found.</param>
        /// <param name="userModelList">The list of users.</param>
        /// <param name="lookUpData">The Shifts look up data.</param>
        /// <returns>A unit of execution.</returns>
        private async Task DeleteOrphanDataShiftsEntityMappingAsync(
            IntegrationApi.SetupDetails configurationDetails,
            List<TeamsShiftMappingEntity> lookUpDataFoundList,
            List<UserDetailsModel> userModelList,
            List<TeamsShiftMappingEntity> lookUpData)
        {
            // Delete entries from orphan list
            var orphanList = lookUpData.Except(lookUpDataFoundList);

            // Iterating over each item in the orphanList.
            foreach (var item in orphanList)
            {
                var user = userModelList.FirstOrDefault(u => u.KronosPersonNumber == item.KronosPersonNumber);
                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configurationDetails.ShiftsAccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", configurationDetails.WFIId);

                if (user != null)
                {
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, $"teams/{user.ShiftTeamId}/schedule/shifts/{item.RowKey}"))
                    {
                        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            var successfulDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                            };

                            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, successfulDeleteProps);

                            await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(item).ConfigureAwait(false);
                        }
                        else
                        {
                            var errorDeleteProps = new Dictionary<string, string>()
                            {
                                { "ResponseCode", response.StatusCode.ToString() },
                                { "ResponseHeader", response.Headers.ToString() },
                            };

                            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, errorDeleteProps);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method that will create a new Shift Mapping entity to store in
        /// Azure table storage.
        /// </summary>
        /// <param name="responseModel">The Shift object response that is received from MS Graph.</param>
        /// <param name="uniqueId">The Kronos Unique ID that is generated.</param>
        /// <param name="user">The user for which the new shift is being created.</param>
        /// <returns>An object of the type <see cref="TeamsShiftMappingEntity"/>.</returns>
        private TeamsShiftMappingEntity CreateNewShiftMappingEntity(
            Models.Response.Shifts.Shift responseModel,
            string uniqueId,
            UserDetailsModel user)
        {
            var createNewShiftMappingEntityProps = new Dictionary<string, string>()
            {
                { "GraphShiftId", responseModel.Id },
                { "GraphShiftEtag", responseModel.ETag },
                { "KronosUniqueId", uniqueId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var startDateTime = DateTime.SpecifyKind(responseModel.SharedShift.StartDateTime.DateTime, DateTimeKind.Utc);
            var endDateTime = DateTime.SpecifyKind(responseModel.SharedShift.EndDateTime.DateTime, DateTimeKind.Utc);

            TeamsShiftMappingEntity shiftMappingEntity = new TeamsShiftMappingEntity
            {
                ETag = responseModel.ETag,
                AadUserId = responseModel.UserId,
                KronosUniqueId = uniqueId,
                KronosPersonNumber = user.KronosPersonNumber,
                ShiftStartDate = startDateTime,
                ShiftEndDate = endDateTime,
            };

            this.telemetryClient.TrackTrace("Creating new shift mapping entity.", createNewShiftMappingEntityProps);

            return shiftMappingEntity;
        }

        /// <summary>
        /// Get all mapped users.
        /// </summary>
        /// <param name="workForceIntegrationId">The workforce integration ID.</param>
        /// <returns>A task.</returns>
        private async Task<IEnumerable<UserDetailsModel>> GetAllMappedUserDetailsAsync(string workForceIntegrationId)
        {
            List<UserDetailsModel> kronosUsers = new List<UserDetailsModel>();

            List<AllUserMappingEntity> mappedUsersResult = await this.userMappingProvider.GetAllActiveMappedUserDetailsAsync().ConfigureAwait(false);

            foreach (var element in mappedUsersResult)
            {
                var teamMappingEntity = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(
                    workForceIntegrationId,
                    element.PartitionKey).ConfigureAwait(false);

                // If team department mapping for a user not present. Skip the user.
                if (teamMappingEntity != null)
                {
                    kronosUsers.Add(new UserDetailsModel
                    {
                        KronosPersonNumber = element.RowKey,
                        ShiftUserId = element.ShiftUserAadObjectId,
                        ShiftTeamId = teamMappingEntity.TeamId,
                        ShiftScheduleGroupId = teamMappingEntity.TeamsScheduleGroupId,
                        KronosTimeZone = teamMappingEntity.KronosTimeZone,
                        OrgJobPath = element.PartitionKey,
                    });
                }
            }

            return kronosUsers;
        }

        private (DateTime KronosStartDateTime, DateTime KronosEndDateTime) GetConvertedShiftDetails(ShiftsShift shift, TeamToDepartmentJobMappingEntity mappedTeam)
        {
            return (
                KronosStartDateTime: this.utility.UTCToKronosTimeZone(
                    (DateTime)(shift.DraftShift?.StartDateTime ?? shift.SharedShift?.StartDateTime), mappedTeam.KronosTimeZone),
                KronosEndDateTime: this.utility.UTCToKronosTimeZone(
                    (DateTime)(shift.DraftShift?.EndDateTime ?? shift.SharedShift?.EndDateTime), mappedTeam.KronosTimeZone));
        }
    }
}