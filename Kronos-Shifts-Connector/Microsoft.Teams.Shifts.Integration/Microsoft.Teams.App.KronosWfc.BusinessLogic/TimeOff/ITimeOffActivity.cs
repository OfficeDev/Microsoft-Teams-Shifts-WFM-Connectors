// <copyright file="ITimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using CommonComments = Microsoft.Teams.App.KronosWfc.Models.CommonEntities.Comments;
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
        /// Fecth time off request details.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="personNumber">The kronos employee id of the user who created request.</param>
        /// <param name="kronosRequestId">The kronos request id.</param>
        /// <returns>Request details response object.</returns>
        /// <remarks>
        /// Due to a bug in Teams this method is not currently used however it is required once
        /// the bug is fixed.
        /// </remarks>
        Task<Response> GetTimeOffRequestDetailsAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string personNumber,
            string kronosRequestId);

        /// <summary>
        /// Send time off request to Kronos API and get response.
        /// </summary>
        /// <param name="jSession">J Session.</param>
        /// <param name="startDateTime">Start Date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="senderMessage">The sender notes of the time off request.</param>
        /// <param name="senderCommentText">The Kronos comment text value to assign to the notes.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time of add response.</returns>
        Task<Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.Response> CreateTimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            string senderMessage,
            string senderCommentText,
            Uri endPointUrl);

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
        /// <returns>A response.</returns>
        Task<CommonResponse> ApproveOrDenyTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId);

        /// <summary>
        /// Updates a time off request with manager comments.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="kronosRequestId">The id of the request.</param>
        /// <param name="startDateTime">Start Date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="managerMessage">The manager notes for the time off request.</param>
        /// <param name="managerCommentText">The Kronos comment text value to assign to the notes.</param>
        /// <param name="existingNotes">Any existing notes attached to the TOR.</param>
        /// <returns>A response.</returns>
        /// <remarks>
        /// Due to a bug in Teams this method is not currently used however it is required once
        /// the bug is fixed.
        /// </remarks>
        Task<CommonResponse> AddManagerCommentsToTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string kronosRequestId,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            string managerMessage,
            string managerCommentText,
            CommonComments existingNotes);
    }
}