// <copyright file="ITimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using CommonResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;

    /// <summary>
    /// TimeOff Activity Interface.
    /// </summary>
    public interface ITimeOffActivity
    {
        /// <summary>
        /// Fecth time off request details for displaying history.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="employees">Employees who created request.</param>
        /// <returns>Request details response object.</returns>
        Task<Response> GetTimeOffRequestDetailsByBatchAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            List<Models.ResponseEntities.HyperFind.ResponseHyperFindResult> employees);

        /// <summary>
        /// Send time off request to Kronos API and get response.
        /// </summary>
        /// <param name="jSession">J Session.</param>
        /// <param name="startDateTime">Start Date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="comments">The Kronos comments for the request.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time off add response.</returns>
        Task<Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.Response> CreateTimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            Comments comments,
            Uri endPointUrl);

        /// <summary>
        /// Submits a time off request which is in draft. The create time off request method creates an entity
        /// in draft mode - this method is thenc alled to make it visible to the manager in kronos.
        /// </summary>
        /// <param name="jSession">jSession object.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reqId">RequestId of the time off request.</param>
        /// <param name="queryDateSpan">Query date span.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time off submit response.</returns>
        Task<Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.SubmitResponse.Response> SubmitTimeOffRequestAsync(
            string jSession,
            string personNumber,
            string reqId,
            string queryDateSpan,
            Uri endPointUrl);

        /// <summary>
        /// Cancels the given time off request.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <param name="kronosId">The id of the TimeOffRequest in Kronos.</param>
        /// <returns>A response.</returns>
        Task<CommonResponse> CancelTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            string kronosId);

        /// <summary>
        /// Approves or Denies the given time off request.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <param name="approved">Whether the request is being accepted or denied.</param>
        /// <param name="kronosId">The id of the TimeOffRequest in Kronos.</param>
        /// <param name="comments">Comments to add to the request.</param>
        /// <returns>A response.</returns>
        Task<CommonResponse> ApproveOrDenyTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId,
            Comments comments);
    }
}