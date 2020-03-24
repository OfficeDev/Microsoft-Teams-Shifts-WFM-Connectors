// <copyright file="IGraphUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface will contain the necessary methods to interface with Microsoft Graph.
    /// </summary>
    public interface IGraphUtility
    {
        /// <summary>
        /// This method will make the call to Microsoft Graph to be able to register the workforce integration.
        /// </summary>
        /// <param name="workforceIntegration">The data to register the workforce integration.</param>
        /// <param name="accessToken">The MS Graph API token.</param>
        /// <returns>A string that contains the necessary response.</returns>
        Task<HttpResponseMessage> RegisterWorkforceIntegrationAsync(
            Models.RequestModels.WorkforceIntegration workforceIntegration,
            string accessToken);

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="accessToken">Access Token.</param>
        /// <returns>The shift user.</returns>
        Task<List<ShiftUser>> FetchShiftUserDetailsAsync(
            string accessToken);

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="accessToken">Access Token.</param>
        /// <returns>The shift user.</returns>
        Task<List<ShiftTeams>> FetchShiftTeamDetailsAsync(
            string accessToken);

        /// <summary>
        /// Method that will get the Microsoft Graph token.
        /// </summary>
        /// <param name="tenantId">The Tenant ID.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="clientId">The App ID of the Configuration Web App.</param>
        /// <param name="clientSecret">The Client Secret.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The string that represents the Microsoft Graph token.</returns>
        Task<string> GetAccessTokenAsync(
            string tenantId,
            string instance,
            string clientId,
            string clientSecret,
            string userId = default(string));

        /// <summary>
        /// Method that will remove the WorkforceIntegrationId from MS Graph.
        /// </summary>
        /// <param name="workforceIntegrationId">The Workforce Integration ID to delete.</param>
        /// <param name="accessToken">The graph access token.</param>
        /// <returns>A unit of exection that represents the response.</returns>
        Task<string> DeleteWorkforceIntegrationAsync(
            string workforceIntegrationId,
            string accessToken);

        /// <summary>
        /// Method that fetchs the scheduling group details based on the shift teams Id provided.
        /// </summary>
        /// <param name="accessToken">The graph access token.</param>
        /// <param name="shiftTeamId">The Shift team Id.</param>
        /// <returns>Shift scheduling group details.</returns>
        Task<string> FetchSchedulingGroupDetailsAsync(
           string accessToken,
           string shiftTeamId);

        /// <summary>
        /// Method that fetchs the scheduling group details based on the shift teams Id provided.
        /// </summary>
        /// <param name="teamsId">The Shift team Id.</param>
        /// <param name="graphClient">The Graph Client.</param>
        /// <param name="wfIID">The Workforce Integration Id.</param>
        /// <param name="accessToken">The MS Graph Access token.</param>
        /// <returns>A unit of execution that contains the success or failure Response.</returns>
        Task<bool> AddWFInScheduleAsync(string teamsId, GraphServiceClient graphClient, string wfIID, string accessToken);
    }
}