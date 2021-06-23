// <copyright file="TimeOffReasonController.cs" company="Microsoft">
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
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.PayCodes;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using TimeOffReasonRequest = Microsoft.Teams.Shifts.Integration.API.Models.Request;
    using TimeOffReasonResponse = Microsoft.Teams.Shifts.Integration.API.Models.Response;

    /// <summary>
    /// Fetch TimeOffReasons from Kronos and create the same in Shifts.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("/api/TimeOffReason")]
    [ApiController]
    public class TimeOffReasonController : ControllerBase
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ITimeOffReasonProvider timeOffReasonProvider;
        private readonly IPayCodeActivity payCodeActivity;
        private readonly AppSettings appSettings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly Utility utility;
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffReasonController"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="azureTableStorageHelper">The Azure Table Storage Helper DI.</param>
        /// <param name="timeOffReasonProvider">The time off reason provider DI.</param>
        /// <param name="payCodeActivity">The paycode activity DI.</param>
        /// <param name="appSettings">Application Settings DI.</param>
        /// <param name="httpClientFactory">Http Client Factory DI.</param>
        /// <param name="teamDepartmentMappingProvider">Team department mapping provider DI.</param>
        /// <param name="utility">Utility common methods DI.</param>
        public TimeOffReasonController(
            TelemetryClient telemetryClient,
            IAzureTableStorageHelper azureTableStorageHelper,
            ITimeOffReasonProvider timeOffReasonProvider,
            IPayCodeActivity payCodeActivity,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            Utility utility)
        {
            this.telemetryClient = telemetryClient;
            this.azureTableStorageHelper = azureTableStorageHelper;
            this.appSettings = appSettings;
            this.payCodeActivity = payCodeActivity;
            this.timeOffReasonProvider = timeOffReasonProvider;
            this.tenantId = this.appSettings?.TenantId;
            this.httpClientFactory = httpClientFactory;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.utility = utility;
        }

        /// <summary>
        /// Maps the Paycode of Kronos with TimeOffReasons.
        /// </summary>
        /// <param name="isRequestFromLogicApp"> If this is the first time sync or not.</param>
        /// <returns>JSONResult.</returns>
        [HttpGet]
        public async Task MapPayCodeTimeOffReasonsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                var result = await this.teamDepartmentMappingProvider.GetMappedTeamToDeptsWithJobPathsAsync().ConfigureAwait(false);
                if (result != null)
                {
                    var kronosReasons = await this.payCodeActivity.FetchPayCodesAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint), allRequiredConfigurations.KronosSession).ConfigureAwait(false);

                    if (kronosReasons == null)
                    {
                        this.telemetryClient.TrackTrace("No paycodes received from Kronos during sync. Please add a paycode to Kronos.");
                        return;
                    }

                    var teams = new List<string>();
                    foreach (var team in result)
                    {
                        if (!teams.Contains(team.TeamId))
                        {
                            teams.Add(team.TeamId);
                        }
                    }

                    if (string.Equals(isRequestFromLogicApp, "false", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await this.InitialTimeOffReasonsSyncAsync(
                                allRequiredConfigurations.ShiftsAccessToken,
                                teams,
                                kronosReasons).ConfigureAwait(false);
                    }

                    await this.UpdateTimeOffReasonsAsync(
                        allRequiredConfigurations.ShiftsAccessToken,
                        teams,
                        this.tenantId,
                        kronosReasons).ConfigureAwait(false);
                }
                else
                {
                    this.telemetryClient.TrackTrace("SyncTimeOffReasonsFromKronosPayCodes - " + Resource.TeamDepartmentDetailsNotFoundMessage);
                }
            }
            else
            {
                this.telemetryClient.TrackTrace("SyncTimeOffReasonsFromKronosPayCodes - " + Resource.SetUpNotDoneMessage);
            }

            this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Creates mapping reasons in storage.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teams">List of team ids.</param>
        /// <param name="kronosReasons">The reasons received from Kronos.</param>
        /// <returns>List of TimeOffReasons.</returns>
        private async Task InitialTimeOffReasonsSyncAsync(
            string accessToken,
            List<string> teams,
            List<string> kronosReasons)
        {
            foreach (var team in teams)
            {
                var initialshiftReasons = await this.GetTimeOffReasonAsync(accessToken, team).ConfigureAwait(false);

                if (initialshiftReasons != null)
                {
                    await this.DeleteMultipleReasons(accessToken, team, initialshiftReasons).ConfigureAwait(false);
                }

                if (kronosReasons != null)
                {
                    await this.AddMultipleReasons(accessToken, team, kronosReasons).ConfigureAwait(false);
                    return;
                }
            }

            return;
        }

        /// <summary>
        /// Update mapping reasons in storage.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="kronosReasons">The reasons received from Kronos.</param>
        /// <returns>An awaitable task.</returns>
        private async Task UpdateTimeOffReasonsAsync(
            string accessToken,
            List<string> teamsIds,
            string tenantId,
            List<string> kronosReasons)
        {
            var mappedReasons = await this.timeOffReasonProvider.FetchReasonsForTeamsAsync(teamsIds[0], tenantId).ConfigureAwait(true);
            var removeActions = new List<Task>();
            var addActions = new List<Task>();

            if (mappedReasons == null)
            {
                return;
            }

            foreach (var team in teamsIds)
            {
                var shiftReasons = await this.GetTimeOffReasonAsync(accessToken, team).ConfigureAwait(false);
                if (shiftReasons == null)
                {
                    return;
                }

                foreach (var shiftReason in shiftReasons)
                {
                    if (!kronosReasons.Contains(shiftReason.DisplayName))
                    {
                        removeActions.Add(this.DeleteSingleReason(accessToken, team, shiftReason));
                    }
                }

                foreach (var reason in kronosReasons)
                {
                    if (shiftReasons.Find(c => c.DisplayName == reason) == null)
                    {
                        addActions.Add(this.AddSingleReason(accessToken, team, reason));
                    }
                }
            }

            await Task.WhenAll(removeActions).ConfigureAwait(false);
            await Task.WhenAll(addActions).ConfigureAwait(false);
            return;
        }

        /// <summary>
        /// Get time off reasons for a team.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <returns>List of TimeOffReasons.</returns>
        private async Task<List<TimeOffReasonResponse.TimeOffReason>> GetTimeOffReasonAsync(string accessToken, string teamsId)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            var graphUrl = this.appSettings.GraphApiUrl;

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (HttpResponseMessage response = await httpClient.GetAsync(new Uri(graphUrl + "teams/" + teamsId + "/schedule/timeOffReasons?$search=\"isActive=true\"")).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (HttpContent content = response.Content)
                    {
                        string result = await content.ReadAsStringAsync().ConfigureAwait(false);

                        if (result != null)
                        {
                            var res = JsonConvert.DeserializeObject<TimeOffReasonResponse.Temp>(result);
                            return res.Value.Where(c => c.IsActive).ToList();
                        }
                    }
                }
                else
                {
                    var failedTimeOffReasonsProps = new Dictionary<string, string>()
                    {
                        { "TeamId", teamsId },
                    };
                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", failedTimeOffReasonsProps);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Create TimeOff Reasons in Shifts.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="payCode">Kronos payCode.</param>
        /// <returns>None.</returns>
        private async Task<(bool, TimeOffReasonResponse.TimeOffReason)> CreateTimeOffReasonAsync(string accessToken, string teamsId, string payCode)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            TimeOffReasonRequest.TimeOffReason timeOffReason = new TimeOffReasonRequest.TimeOffReason
            {
                DisplayName = payCode,
                IconType = "plane",
                IsActive = true,
            };
            var requestString = JsonConvert.SerializeObject(timeOffReason);

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + teamsId + "/schedule/timeOffReasons")
            {
                Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
            })
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(true);
                if (response.IsSuccessStatusCode)
                {
                    var createdTimeOffReason = JsonConvert.DeserializeObject<TimeOffReasonResponse.TimeOffReason>(
                        await response.Content.ReadAsStringAsync().ConfigureAwait(true));
                    return (true, createdTimeOffReason);
                }
                else
                {
                    var failedCreateTimeOffReasonsProps = new Dictionary<string, string>()
                    {
                        { "TeamId", teamsId },
                        { "PayCode", payCode },
                    };

                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", failedCreateTimeOffReasonsProps);
                    return (false, null);
                }
            }
        }

        private async Task<bool> DeleteTimeOffReasonAsync(string accessToken, string teamsId, string timeOffId)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, "teams/" + teamsId + "/schedule/timeOffReasons/" + timeOffId))
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(true);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var failedCreateTimeOffReasonsProps = new Dictionary<string, string>()
                    {
                        { "TeamId", teamsId },
                        { "TeamOffId", timeOffId },
                    };

                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", failedCreateTimeOffReasonsProps);
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes all reasons in Shifts except any given.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="reasons">The list of reasons to edit.</param>
        private async Task DeleteMultipleReasons(string accessToken, string teamsId, List<TimeOffReasonResponse.TimeOffReason> reasons)
        {
            var successfullyRemovedReasons = new List<string>();
            foreach (var reason in reasons)
            {
                if (await this.DeleteTimeOffReasonAsync(accessToken, teamsId, reason.Id).ConfigureAwait(false))
                {
                    successfullyRemovedReasons.Add(reason.DisplayName);
                }
            }

            await this.timeOffReasonProvider.DeleteSpecificReasons(successfullyRemovedReasons.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a given reason to both Shifts and the Database.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="reason">The name of the reason.</param>
        private async Task AddSingleReason(string accessToken, string teamsId, string reason)
        {
            (var success, TimeOffReasonResponse.TimeOffReason reasonToAdd) = await this.CreateTimeOffReasonAsync(accessToken, teamsId, reason).ConfigureAwait(false);
            if (success)
            {
                var paycodeMapping = new PayCodeToTimeOffReasonsMappingEntity
                {
                    PartitionKey = teamsId,
                    RowKey = reasonToAdd.DisplayName,
                    TimeOffReasonId = reasonToAdd.Id,
                };

                await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(paycodeMapping, "PayCodeToTimeOffReasonsMapping").ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Deletes a given reason to both Shifts and the Database.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="reason">The reason.</param>
        private async Task DeleteSingleReason(string accessToken, string teamsId, TimeOffReasonResponse.TimeOffReason reason)
        {
            if (await this.DeleteTimeOffReasonAsync(accessToken, teamsId, reason.Id).ConfigureAwait(false))
            {
                await this.timeOffReasonProvider.DeleteSpecificReasons(reason.DisplayName).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds a list of given reasons to both Shifts and the Database.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="kronosReasons">The list of reasons pulled from Kronos.</param>
        private async Task AddMultipleReasons(string accessToken, string teamsId, List<string> kronosReasons)
        {
            foreach (var reason in kronosReasons)
            {
                await this.AddSingleReason(accessToken, teamsId, reason).ConfigureAwait(false);
            }
        }
    }
}
