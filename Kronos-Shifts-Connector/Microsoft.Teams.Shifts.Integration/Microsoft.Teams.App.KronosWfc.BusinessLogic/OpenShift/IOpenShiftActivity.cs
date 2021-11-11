﻿// <copyright file="IOpenShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;
    using OpenShiftResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.Response;

    /// <summary>
    /// OpenShift Activity interface.
    /// </summary>
    public interface IOpenShiftActivity
    {
        /// <summary>
        /// Create an open shift in Kronos.
        /// </summary>
        /// <param name="endpoint">The endpoint for the request.</param>
        /// <param name="jSession">The Jsession token.</param>
        /// <param name="shiftStartDate">The start date of the open shift.</param>
        /// <param name="shiftEndDate">The end date of the open shift.</param>
        /// <param name="overADateBorder">Whether the open shift spans over a date border.</param>
        /// <param name="jobPath">The job of the open shift.</param>
        /// <param name="openShiftlabel">The open shift label.</param>
        /// <param name="startTime">The start time of the open shift.</param>
        /// <param name="endTime">The end time of the open shift.</param>
        /// <param name="slotCount">The number of open shifts to create.</param>
        /// <param name="comments">The comments for the open shift.</param>
        /// <returns>A task containing the response.</returns>
        Task<OpenShiftResponse> CreateOpenShiftAsync(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string openShiftlabel,
            string startTime,
            string endTime,
            int slotCount,
            Comments comments);

        /// <summary>
        /// Fetch open shifts from Kronos in a batch manner.
        /// </summary>
        /// <param name="endpointUrl">The Kronos WFC API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="orgJobPathsBatchList">The list of Org Job Paths.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <returns>A response object.</returns>
        Task<OpenShiftResponse> GetOpenShiftDetailsInBatchAsync(
            Uri endpointUrl,
            string jSession,
            List<string> orgJobPathsBatchList,
            string queryDateSpan);

        /// <summary>
        /// Method to create the DraftOpenShift.
        /// </summary>
        /// <param name="tenantId">The TenantId.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="openShiftObject">The OpenShiftObj.</param>
        /// <param name="endPointUrl">The Kronos WFC API.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.Response> PostDraftOpenShiftRequestAsync(
            string tenantId,
            string jSession,
            OpenShiftObj openShiftObject,
            Uri endPointUrl);

        /// <summary>
        /// Method that will return the response post updating the status for an open shift request.
        /// </summary>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="requestId">The request ID.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="endpointUrl">The Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.Response> PostOpenShiftRequestStatusUpdateAsync(
            string personNumber,
            string requestId,
            string queryDateSpan,
            string comment,
            Uri endpointUrl,
            string jSession);

        /// <summary>
        /// Method definition to get approved or denied open shift requests per user.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <returns>A unit of execution.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response> GetApprovedOrDeclinedOpenShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber);

        /// <summary>
        /// Approves or Denies the given request.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <param name="approved">Whether the request is being accepted or denied.</param>
        /// <param name="kronosId">The id of the OpenShiftRequest in Kronos.</param>
        /// <param name="comments">The comments to add to the request.</param>
        /// <returns>A response.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response> ApproveOrDenyOpenShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId,
            Comments comments);
    }
}