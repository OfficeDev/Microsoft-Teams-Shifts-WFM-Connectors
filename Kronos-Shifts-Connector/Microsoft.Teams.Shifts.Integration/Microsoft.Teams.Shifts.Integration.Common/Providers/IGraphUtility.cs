// <copyright file="IGraphUtility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.Graph;

    /// <summary>
    /// This interface will contain the necessary methods to interface with Microsoft Graph.
    /// </summary>
    public interface IGraphUtility
    {
        /// <summary>
        /// This method will make the call to Microsoft Graph to be able to register the workforce integration.
        /// </summary>
        /// <param name="workforceIntegration">The data to register the workforce integration.</param>
        /// <param name="graphConfigurationDetails">The Graph configuration details.</param>
        /// <returns>A string that contains the necessary response.</returns>
        Task<HttpResponseMessage> RegisterWorkforceIntegrationAsync(Models.RequestModels.WorkforceIntegration workforceIntegration, GraphConfigurationDetails graphConfigurationDetails);

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="graphConfigurationDetails">Graph configuration details.</param>
        /// <returns>The shift user.</returns>
        Task<List<ShiftUser>> FetchShiftUserDetailsAsync(GraphConfigurationDetails graphConfigurationDetails);

        /// <summary>
        /// Share the schedule between the given date range.
        /// </summary>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <param name="teamId">The id of the team whos schedule we want to share.</param>
        /// <param name="startDateTime">The start time we want to share from.</param>
        /// <param name="endDateTime">The end time we want to share until.</param>
        /// <param name="notifyTeam">Whether we want to notify the team or not.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> ShareSchedule(GraphConfigurationDetails graphConfigurationDetails, string teamId, DateTime startDateTime, DateTime endDateTime, bool notifyTeam);

        /// <summary>
        /// Fetch user details for shifts using graph api tokens.
        /// </summary>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <returns>The shift user.</returns>
        Task<List<ShiftTeams>> FetchShiftTeamDetailsAsync(GraphConfigurationDetails graphConfigurationDetails);

        /// <summary>
        /// Sends a graph request.
        /// </summary>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="httpMethod">The http method.</param>
        /// <param name="requestUrl">The request URI.</param>
        /// <param name="requestString">The request string to add as the request content.</param>
        /// <returns>A HttpResponseMessage object.</returns>
        Task<HttpResponseMessage> SendGraphHttpRequest(GraphConfigurationDetails graphConfigurationDetails, HttpClient httpClient, HttpMethod httpMethod, string requestUrl, string requestString = null);

        /// <summary>
        /// Method that will get the Microsoft Graph token.
        /// </summary>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <returns>The string that represents the Microsoft Graph token.</returns>
        Task<string> GetAccessTokenAsync(GraphConfigurationDetails graphConfigurationDetails);

        /// <summary>
        /// Method that will remove the WorkforceIntegrationId from MS Graph.
        /// </summary>
        /// <param name="workforceIntegrationId">The Workforce Integration ID to delete.</param>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <returns>A unit of exection that represents the response.</returns>
        Task<string> DeleteWorkforceIntegrationAsync(string workforceIntegrationId, GraphConfigurationDetails graphConfigurationDetails);

        /// <summary>
        /// Method that fetchs the scheduling group details based on the shift teams Id provided.
        /// </summary>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <param name="shiftTeamId">The Shift team Id.</param>
        /// <returns>Shift scheduling group details.</returns>
        Task<string> FetchSchedulingGroupDetailsAsync(GraphConfigurationDetails graphConfigurationDetails, string shiftTeamId);

        /// <summary>
        /// Method that fetchs the scheduling group details based on the shift teams Id provided.
        /// </summary>
        /// <param name="teamsId">The Shift team Id.</param>
        /// <param name="wfIID">The Workforce Integration Id.</param>
        /// <param name="graphConfigurationDetails">The graph configuration details.</param>
        /// <returns>A unit of execution that contains the success or failure Response.</returns>
        Task<bool> AddWFInScheduleAsync(string teamsId, string wfIID, GraphConfigurationDetails graphConfigurationDetails);
    }
}