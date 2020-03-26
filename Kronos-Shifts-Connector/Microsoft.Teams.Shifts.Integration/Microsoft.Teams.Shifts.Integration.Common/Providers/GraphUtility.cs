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
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Cache;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
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

        /// <summary>
        /// Method to call the Graph API to register the workforce integration.
        /// </summary>
        /// <param name="workforceIntegration">The workforce integration.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>The JSON response of the Workforce Integration registration.</returns>
        public async Task<HttpResponseMessage> RegisterWorkforceIntegrationAsync(
            Models.RequestModels.WorkforceIntegration workforceIntegration,
            string accessToken)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace(BusinessLogicResource.RegisterWorkforceIntegrationAsync + " called at " + DateTime.Now.ToString("o", provider));

            var requestString = JsonConvert.SerializeObject(workforceIntegration);
            var httpClientWFI = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClientWFI.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClientWFI.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teamwork/workforceIntegrations")
            {
                Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
            })
            {
                var response = await httpClientWFI.SendAsync(httpRequestMessage).ConfigureAwait(false);
                return response;
            }
        }

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="accessToken">Access Token.</param>
        /// <returns>The shift user.</returns>
        public async Task<List<ShiftUser>> FetchShiftUserDetailsAsync(
            string accessToken)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftUserDetailsAsync, fetchShiftUserDetailsProps);

            List<ShiftUser> shiftUsers = new List<ShiftUser>();
            bool hasMoreUsers = false;

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestUri = "users";
            do
            {
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var jsonResult = JsonConvert.DeserializeObject<ShiftUserModel>(responseContent);
                        shiftUsers.AddRange(jsonResult.Value);
                        if (jsonResult.NextLink != null)
                        {
                            hasMoreUsers = true;
                            requestUri = jsonResult.NextLink.ToString();
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
            }
            while (hasMoreUsers == true);

            return shiftUsers;
        }

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="accessToken">Access Token.</param>
        /// <returns>The shift user.</returns>
        public async Task<List<ShiftTeams>> FetchShiftTeamDetailsAsync(
            string accessToken)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            List<ShiftTeams> shiftTeams = new List<ShiftTeams>();
            var hasMoreTeams = false;

            this.telemetryClient.TrackTrace(BusinessLogicResource.FetchShiftTeamDetailsAsync, fetchShiftUserDetailsProps);
            var hcfClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            hcfClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            hcfClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Filter group who has associated teams also.
            var requestUri = "groups?$filter=resourceProvisioningOptions/Any(x:x eq 'Team')";
            do
            {
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    var response = await hcfClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var allResponse = JsonConvert.DeserializeObject<AllShiftsTeam>(responseContent);
                        shiftTeams.AddRange(allResponse.ShiftTeams);

                        // If Shifts has more Teams. Typically graph API has 100 teams in one batch.
                        // Using nextlink, teams from next batch is fetched.
                        if (allResponse.NextLink != null)
                        {
                            requestUri = allResponse.NextLink.ToString();
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
            }

            // loop until Shifts has more teams to fetch.
            while (hasMoreTeams);

            return shiftTeams;
        }

        /// <summary>
        /// Method that will obtain the Graph token.
        /// </summary>
        /// <param name="tenantId">The Tenant ID.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="clientId">The App ID of the Web Application.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="userId">The AAD Object ID of the Admin, could also be the UPN.</param>
        /// <returns>A string that represents the Microsoft Graph API token.</returns>
        public async Task<string> GetAccessTokenAsync(
            string tenantId,
            string instance,
            string clientId,
            string clientSecret,
            string userId = default(string))
        {
            string authority = $"{instance}{tenantId}";
            var cache = new RedisTokenCache(this.cache, clientId);
            var authContext = new AuthenticationContext(authority, cache);
            var userIdentity = new UserIdentifier(userId, UserIdentifierType.UniqueId);

            try
            {
                var result = await authContext.AcquireTokenSilentAsync(
                    "https://graph.microsoft.com",
                    new ClientCredential(
                        clientId,
                        clientSecret),
                    userIdentity).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (AdalException adalEx)
            {
                this.telemetryClient.TrackException(adalEx);
                var retryResult = await authContext.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    new ClientCredential(
                        clientId,
                        clientSecret)).ConfigureAwait(false);
                return retryResult.AccessToken;
            }
        }

        /// <summary>
        /// Method that will delete the workforce integration from MS Graph.
        /// </summary>
        /// <param name="workforceIntegrationId">The workforce integration ID to delete.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        public async Task<string> DeleteWorkforceIntegrationAsync(
            string workforceIntegrationId,
            string accessToken)
        {
            var wfiDeletionProps = new Dictionary<string, string>()
            {
                { "WorkforceIntegrationId", workforceIntegrationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, wfiDeletionProps);

            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, $"teamwork/workforceIntegrations/{workforceIntegrationId}")
            {
            })
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
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
        }

        /// <summary>
        /// Method that fetchs the scheduling group details based on the shift teams Id provided.
        /// </summary>
        /// <param name="accessToken">Access Token.</param>
        /// <param name="shiftTeamId">Shift Team Id.</param>
        /// <returns>Shift scheduling group details.</returns>
        public async Task<string> FetchSchedulingGroupDetailsAsync(
            string accessToken,
            string shiftTeamId)
        {
            var fetchShiftUserDetailsProps = new Dictionary<string, string>()
            {
                { "IncomingShiftsTeamId", shiftTeamId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, fetchShiftUserDetailsProps);
            var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "teams/" + shiftTeamId + "/schedule/schedulingGroups")
            {
                Headers =
                    {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {accessToken}" },
                    },
            })
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return responseContent;
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
        }

        /// <summary>
        /// Method to update the Workforce Integration ID to the schedule.
        /// </summary>
        /// <param name="teamsId">The Shift team Id.</param>
        /// <param name="graphClient">The Graph Client.</param>
        /// <param name="wfIID">The Workforce Integration Id.</param>
        /// <param name="accessToken">The MS Graph Access token.</param>
        /// <returns>A unit of execution that contains the success or failure Response.</returns>
        public async Task<bool> AddWFInScheduleAsync(
            string teamsId,
            GraphServiceClient graphClient,
            string wfIID,
            string accessToken)
        {
            if (graphClient is null)
            {
                throw new ArgumentNullException(nameof(graphClient));
            }

            try
            {
                var sched = await graphClient.Teams[teamsId].Schedule
                            .Request()
                            .GetAsync().ConfigureAwait(false);

                var schedule = new Schedule
                {
                    Enabled = true,
                    TimeZone = sched.TimeZone,
                    WorkforceIntegrationIds = new List<string>() { wfIID },
                };

                var requestString = JsonConvert.SerializeObject(schedule);
                var httpClient = this.httpClientFactory.CreateClient("GraphBetaAPI");
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, "teams/" + teamsId + "/schedule")
                {
                    Headers =
                    {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {accessToken}" },
                    },
                    Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
                })
                {
                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return true;
                    }
                    else
                    {
                        var failedResponseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var failedResponseProps = new Dictionary<string, string>()
                        {
                            { "FailedResponse", failedResponseContent },
                        };

                        this.telemetryClient.TrackTrace(BusinessLogicResource.AddWorkForceIntegrationToSchedule, failedResponseProps);
                        return false;
                    }
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