// <copyright file="GraphUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Graph;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Teams.Shifts.Integration.API.Models.Request;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Cache;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the methods that are defined in <see cref="IGraphUtility"/>.
    /// </summary>
    public class GraphUtility : IGraphUtility
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IDistributedCache cache;
        private readonly System.Net.Http.IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphUtility"/> class.
        /// </summary>
        /// <param name="telemetryClient">The application insights DI.</param>
        /// <param name="cache">Cache.</param>
        /// <param name="httpClientFactory">Http Client Factory DI.</param>
        public GraphUtility(
            TelemetryClient telemetryClient,
            IDistributedCache cache,
            System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            this.telemetryClient = telemetryClient;
            this.cache = cache;
            this.httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> RegisterWorkforceIntegrationAsync(Models.RequestModels.WorkforceIntegration workforceIntegration, GraphConfigurationDetails graphConfigurationDetails)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace(BusinessLogicResource.RegisterWorkforceIntegrationAsync + " called at " + DateTime.Now.ToString("o", provider));

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

            var requestUrl = "teamwork/workforceIntegrations";
            var requestString = JsonConvert.SerializeObject(workforceIntegration);

            return await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Post, requestUrl, requestString).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<ShiftUser>> FetchShiftUserDetailsAsync(GraphConfigurationDetails graphConfigurationDetails)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftUserDetailsAsync, fetchShiftUserDetailsProps);

            List<ShiftUser> shiftUsers = new List<ShiftUser>();
            bool hasMoreUsers = false;

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

            var requestUrl = "users";

            do
            {
                var response = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Get, requestUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var jsonResult = JsonConvert.DeserializeObject<ShiftUserModel>(responseContent);
                    shiftUsers.AddRange(jsonResult.Value);
                    if (jsonResult.NextLink != null)
                    {
                        hasMoreUsers = true;
                        requestUrl = jsonResult.NextLink.ToString();
                    }
                    else
                    {
                        hasMoreUsers = false;
                    }
                }
                else
                {
                    var failedResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedResponseContent },
                        };

                    this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftUserDetailsAsync, failedResponseProps);
                }
            }
            while (hasMoreUsers == true);

            return shiftUsers;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> ShareSchedule(GraphConfigurationDetails graphConfigurationDetails, string teamId, DateTime startDateTime, DateTime endDateTime, bool notifyTeam)
        {
            var shareRequest = new ShareSchedule
            {
                NotifyTeam = notifyTeam,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
            };

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestUrl = $"teams/{teamId}/schedule/share";
            var requestString = JsonConvert.SerializeObject(shareRequest);

            return await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Post, requestUrl, requestString).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<ShiftTeams>> FetchShiftTeamDetailsAsync(GraphConfigurationDetails graphConfigurationDetails)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            List<ShiftTeams> shiftTeams = new List<ShiftTeams>();
            var hasMoreTeams = false;

            this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftTeamDetailsAsync, fetchShiftUserDetailsProps);

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

            // Filter group who has associated teams also.
            var requestUrl = "groups?$filter=resourceProvisioningOptions/Any(x:x eq 'Team')";

            do
            {
                var response = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Get, requestUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var allResponse = JsonConvert.DeserializeObject<AllShiftsTeam>(responseContent);
                    shiftTeams.AddRange(allResponse.ShiftTeams);

                    // If Shifts has more Teams. Typically graph API has 100 teams in one batch.
                    // Using nextlink, teams from next batch is fetched.
                    if (allResponse.NextLink != null)
                    {
                        requestUrl = allResponse.NextLink.ToString();
                        hasMoreTeams = true;
                    }

                    // nextlink is null when there are no batch of teams to be fetched.
                    else
                    {
                        hasMoreTeams = false;
                    }
                }
                else
                {
                    hasMoreTeams = false;

                    var failedResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedResponseContent },
                        };

                    this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftTeamDetailsAsync, failedResponseProps);
                }
            }

            // loop until Shifts has more teams to fetch.
            while (hasMoreTeams);

            return shiftTeams;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> SendHttpRequest(GraphConfigurationDetails graphConfigurationDetails, HttpClient httpClient, HttpMethod httpMethod, string requestUrl, string requestString = null)
        {
            using (var httpRequestMessage = new HttpRequestMessage(httpMethod, requestUrl))
            {
                if (!string.IsNullOrEmpty(requestString))
                {
                    httpRequestMessage.Content = new StringContent(requestString, Encoding.UTF8, "application/json");
                }

                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Refresh the access token and recall
                    graphConfigurationDetails.ShiftsAccessToken = await this.GetAccessTokenAsync(graphConfigurationDetails).ConfigureAwait(false);

                    using (var retryRequestMessage = new HttpRequestMessage(httpMethod, requestUrl))
                    {
                        if (!string.IsNullOrEmpty(requestString))
                        {
                            retryRequestMessage.Content = new StringContent(requestString, Encoding.UTF8, "application/json");
                        }

                        retryRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);
                        return await httpClient.SendAsync(retryRequestMessage).ConfigureAwait(false);
                    }
                }

                return response;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenAsync(GraphConfigurationDetails graphConfigurationDetails)
        {
            string authority = $"{graphConfigurationDetails.Instance}{graphConfigurationDetails.TenantId}";
            var cache = new RedisTokenCache(this.cache, graphConfigurationDetails.ClientId);
            var authContext = new AuthenticationContext(authority, cache);
            var userIdentity = new UserIdentifier(graphConfigurationDetails.ShiftsAdminAadObjectId, UserIdentifierType.UniqueId);

            try
            {
                var result = await authContext.AcquireTokenSilentAsync(
                    "https://graph.microsoft.com",
                    new ClientCredential(
                        graphConfigurationDetails.ClientId,
                        graphConfigurationDetails.ClientSecret),
                    userIdentity).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (AdalException adalEx)
            {
                this.telemetryClient.TrackException(adalEx);
                var retryResult = await authContext.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    new ClientCredential(
                        graphConfigurationDetails.ClientId,
                        graphConfigurationDetails.ClientSecret)).ConfigureAwait(false);
                return retryResult.AccessToken;
            }
        }

        /// <inheritdoc/>
        public async Task<string> DeleteWorkforceIntegrationAsync(string workforceIntegrationId, GraphConfigurationDetails graphConfigurationDetails)
        {
            var wfiDeletionProps = new Dictionary<string, string>()
            {
                { "WorkforceIntegrationId", workforceIntegrationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, wfiDeletionProps);

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

            var requestUrl = $"teamwork/workforceIntegrations/{workforceIntegrationId}";

            var response = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Delete, requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response.StatusCode.ToString();
            }
            else
            {
                var failedResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var failedResponseProps = new Dictionary<string, string>()
                    {
                        { "FailedResponse", failedResponseContent },
                        { "WorkForceIntegrationId", workforceIntegrationId },
                    };

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, failedResponseProps);
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<string> FetchSchedulingGroupDetailsAsync(GraphConfigurationDetails graphConfigurationDetails, string shiftTeamId)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "IncomingShiftsTeamId", shiftTeamId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, fetchShiftUserDetailsProps);

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

            var requestUrl = $"teams/{shiftTeamId}/schedule/schedulingGroups";

            var response = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Get, requestUrl).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                var failedResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedResponseContent },
                            { "ShiftTeamId", shiftTeamId },
                        };

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, failedResponseProps);
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AddWFInScheduleAsync(string teamsId, string wfIID, GraphConfigurationDetails graphConfigurationDetails)
        {
            try
            {
                var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

                var scheduleRequestUrl = $"teams/{teamsId}/schedule";

                var getScheduleResponse = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Get, scheduleRequestUrl).ConfigureAwait(false);
                if (!getScheduleResponse.IsSuccessStatusCode)
                {
                    var failedScheduleResponseContent = await getScheduleResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedScheduleResponseContent },
                        };

                    this.telemetryClient.TrackTrace($"{BusinessLogicResource.AddWorkForceIntegrationToSchedule} - Failed to retrieve schedule.", failedResponseProps);
                    return false;
                }

                var scheduleResponseContent = await getScheduleResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var scheduleResponse = JsonConvert.DeserializeObject<Schedule>(scheduleResponseContent);

                var schedule = new Schedule
                {
                    Enabled = true,
                    TimeZone = scheduleResponse.TimeZone,
                    WorkforceIntegrationIds = new List<string>() { wfIID },
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphConfigurationDetails.ShiftsAccessToken);

                var addWfiRequestUrl = $"teams/{teamsId}/schedule";
                var addWfiRequestString = JsonConvert.SerializeObject(schedule);

                var addWfiResponse = await this.SendHttpRequest(graphConfigurationDetails, httpClient, HttpMethod.Put, addWfiRequestUrl, addWfiRequestString).ConfigureAwait(false);
                if (addWfiResponse.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var failedResponseContent = await addWfiResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedResponseContent },
                        };

                    this.telemetryClient.TrackTrace($"{BusinessLogicResource.AddWorkForceIntegrationToSchedule} - Failed to add WFI Id to schedule.", failedResponseProps);
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}