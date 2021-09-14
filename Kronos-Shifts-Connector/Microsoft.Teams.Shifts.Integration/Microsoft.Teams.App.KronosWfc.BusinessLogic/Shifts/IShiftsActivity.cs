// <copyright file="IUpcomingShiftsActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using CRUDResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using UpcomingShifts = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts;

    /// <summary>
    /// Upcoming shift activity interface.
    /// </summary>
    public interface IShiftsActivity
    {
        /// <summary>
        /// Shows upcoming shifts.
        /// </summary>
        /// <param name="endPointUrl">end Point Url.</param>
        /// <param name="jSession">jSession object.</param>
        /// <param name="startDate">Start Date.</param>
        /// <param name="endDate">End Date.</param>
        /// <param name="employees">Employees shift data.</param>
        /// <returns>Upcoming shifts response.</returns>
        Task<UpcomingShifts.Response> ShowUpcomingShiftsInBatchAsync(
            Uri endPointUrl,
            string jSession,
            string startDate,
            string endDate,
            List<ResponseHyperFindResult> employees);

        /// <summary>
        /// Creates a shift in Kronos.
        /// </summary>
        /// <param name="endpoint">The endpoint for the request.</param>
        /// <param name="jSession">The Jsession token.</param>
        /// <param name="shiftStartDate">The start date of the shift.</param>
        /// <param name="shiftEndDate">The end date of the shift.</param>
        /// <param name="overADateBorder">Whether the shift spans over a date border.</param>
        /// <param name="jobPath">The job of the shift.</param>
        /// <param name="kronosId">The id of the employee.</param>
        /// <param name="startTime">The start time of the shift.</param>
        /// <param name="endTime">The end time of the shift.</param>
        /// <param name="shiftComments">The shift comments object.</param>
        /// <returns>A task containing the response.</returns>
        Task<CRUDResponse> CreateShift(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime,
            Comments shiftComments);

        /// <summary>
        /// Edits a shift in Kronos.
        /// </summary>
        /// <param name="endpoint">The endpoint for the request.</param>
        /// <param name="jSession">The Jsession token.</param>
        /// <param name="replacementShiftStartDate">The start date of the shift.</param>
        /// <param name="replacementShiftEndDate">The end date of the shift.</param>
        /// <param name="overADateBorder">Whether the shift spans over a date border.</param>
        /// <param name="jobPath">The job of the shift.</param>
        /// <param name="kronosId">The id of the employee.</param>
        /// <param name="replacementShiftStartTime">The start time of the shift.</param>
        /// <param name="replacementShiftEndTime">The end time of the shift.</param>
        /// <param name="shiftToReplaceStartDate">The start date of the shift we want to replace.</param>
        /// <param name="shiftToReplaceEndDate">The end date of the shift we want to replace.</param>
        /// <param name="shiftToReplaceStartTime">The start time of the shift we want to replace.</param>
        /// <param name="shiftToReplaceEndTime">The end time of the shift we want to replace.</param>
        /// <param name="comments">The comments for the shift.</param>
        /// <returns>A task containing the response.</returns>
        Task<CRUDResponse> EditShift(
            Uri endpoint,
            string jSession,
            string replacementShiftStartDate,
            string replacementShiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string replacementShiftStartTime,
            string replacementShiftEndTime,
            string shiftToReplaceStartDate,
            string shiftToReplaceEndDate,
            string shiftToReplaceStartTime,
            string shiftToReplaceEndTime,
            Comments comments);

        /// <summary>
        /// Deletes a shift in Kronos.
        /// </summary>
        /// <param name="endpoint">The endpoint for the request.</param>
        /// <param name="jSession">The Jsession token.</param>
        /// <param name="shiftStartDate">The start date of the shift.</param>
        /// <param name="shiftEndDate">The end date of the shift.</param>
        /// <param name="overADateBorder">Whether the shift spans over a date border.</param>
        /// <param name="jobPath">The job of the shift.</param>
        /// <param name="kronosId">The id of the employee.</param>
        /// <param name="startTime">The start time of the shift.</param>
        /// <param name="endTime">The end time of the shift.</param>
        /// <returns>A task containing the response.</returns>
        Task<CRUDResponse> DeleteShift(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime);
    }
}