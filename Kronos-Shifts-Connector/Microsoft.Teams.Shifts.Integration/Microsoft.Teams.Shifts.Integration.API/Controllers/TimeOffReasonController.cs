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
        /// <returns>JSONResult.</returns>
        [HttpGet]
        public async Task MapPayCodeTimeOffReasonsAsync()
        {
            this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                var result = await this.teamDepartmentMappingProvider.GetMappedTeamToDeptsWithJobPathsAsync().ConfigureAwait(false);
                if (result != null)
                {
                    foreach (var team in result)
                    {
                        await this.UpdateTimeOffReasonsAsync(
                            allRequiredConfigurations.ShiftsAccessToken,
                            team.TeamId,
                            this.tenantId,
                            allRequiredConfigurations.WfmEndPoint,
                            allRequiredConfigurations.KronosSession).ConfigureAwait(false);
                    }
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
        /// Update mapping reasons in storage.
        /// </summary>
        /// <param name="accessToken">Cached AccessToken.</param>
        /// <param name="teamsId">MS Teams Id.</param>
        /// <param name="tenantId">tenant Id.</param>
        /// <param name="kronosEndpoint">The Kronos WFC API endpoint.</param>
        /// <param name="kronosSession">The Kronos WFC Jsession.</param>
        /// <returns>List of TimeOffReasons.</returns>
        private async Task<dynamic> UpdateTimeOffReasonsAsync(
            string accessToken,
            string teamsId,
            string tenantId,
            string kronosEndpoint,
            string kronosSession)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            var paycodeList = await this.payCodeActivity.FetchPayCodesAsync(new Uri(kronosEndpoint), kronosSession).ConfigureAwait(true);
            var lstTimeOffReasons = await this.GetTimeOffReasonAsync(accessToken, teamsId).ConfigureAwait(true);
            if (lstTimeOffReasons != null)
            {
                var reasonList = lstTimeOffReasons.Select(c => c.DisplayName).ToList();
                var newCodes = paycodeList.Except(reasonList);

                foreach (var payCode in newCodes)
                {
                    await this.CreateTimeOffReasonAsync(accessToken, teamsId, payCode).ConfigureAwait(true);
                }

                var timeOffReasons = await this.GetTimeOffReasonAsync(accessToken, teamsId).ConfigureAwait(true);
                var mappedReasons = await this.timeOffReasonProvider.FetchReasonsForTeamsAsync(teamsId, tenantId).ConfigureAwait(true);
                foreach (var timeOffReason in timeOffReasons)
                {
                    if ((paycodeList.Contains(timeOffReason.DisplayName) && !mappedReasons.ContainsKey(timeOffReason.Id))
                    || (paycodeList.Contains(timeOffReason.DisplayName)
                        && mappedReasons.ContainsKey(timeOffReason.Id)
                        && !mappedReasons[timeOffReason.Id].Contains(timeOffReason.DisplayName, StringComparison.InvariantCulture)))
                    {
                        var payCodeToTimeOffReasonsMappingEntity = new PayCodeToTimeOffReasonsMappingEntity
                        {
                            PartitionKey = teamsId,
                            RowKey = timeOffReason.DisplayName,
                            TimeOffReasonId = timeOffReason.Id,
                        };

                        await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(payCodeToTimeOffReasonsMappingEntity, "PayCodeToTimeOffReasonsMapping").ConfigureAwait(true);
                    }
                }
            }

            return null;
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
                            return res.Value.Where(c => c.IsActive == true).ToList();
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
        private async Task<dynamic> CreateTimeOffReasonAsync(string accessToken, string teamsId, string payCode)
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
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    return responseContent;
                }
                else
                {
                    var failedCreateTimeOffReasonsProps = new Dictionary<string, string>()
                    {
                        { "TeamId", teamsId },
                        { "PayCode", payCode },
                    };

                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}", failedCreateTimeOffReasonsProps);
                    return string.Empty;
                }
            }
        }
    }
}