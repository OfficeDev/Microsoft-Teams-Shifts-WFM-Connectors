// <copyright file="ICreateTimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.ShiftsToKronos.CreateTimeOff
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Create TimeOff Activity Interface.
    /// </summary>
    public interface ICreateTimeOffActivity
    {
        /// <summary>
        /// Submit time of request which is in draft.
        /// </summary>
        /// <param name="jSession">jSession object.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reqId">RequestId of the time off request.</param>
        /// <param name="queryDateSpan">Query date span.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time of submit response.</returns>
        Task<Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.SubmitResponse.Response> SubmitTimeOffRequestAsync(
            string jSession, string personNumber, string reqId, string queryDateSpan, Uri endPointUrl);

        /// <summary>
        /// Send time off request to Kronos API and get response.
        /// </summary>
        /// <param name="jSession">J Session.</param>
        /// <param name="startDateTime">Start Date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time of add response.</returns>
        Task<Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.Response> CreateTimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            Uri endPointUrl);
    }
}