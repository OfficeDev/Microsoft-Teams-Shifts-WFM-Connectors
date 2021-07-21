// <copyright file="IUpcomingShiftsActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using CRUDRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts.ShiftRequest;
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
        /// <param name="shiftDate">The date of the shift.</param>
        /// <param name="jobPath">The job of the shift.</param>
        /// <param name="kronosId">The id of the employee.</param>
        /// <param name="shiftLabel">The label for the shift.</param>
        /// <param name="startTime">The start time of the shift.</param>
        /// <param name="endTime">The end time of the shift.</param>
        /// <returns>A task containing the response.</returns>
        Task<CRUDResponse> CreateShift(
            Uri endpoint,
            string jSession,
            string shiftDate,
            string jobPath,
            string kronosId,
            string shiftLabel,
            string startTime,
            string endTime); 

        /// <summary>
        /// Deletes a shift in Kronos.
        /// </summary>
        /// <param name="endpoint">The endpoint for the request.</param>
        /// <param name="jSession">The Jsession token.</param>
        /// <param name="shiftDate">The date of the shift.</param>
        /// <param name="jobPath">The job of the shift.</param>
        /// <param name="kronosId">The id of the employee.</param>
        /// <param name="startTime">The start time of the shift.</param>
        /// <param name="endTime">The end time of the shift.</param>
        /// <returns>A task containing the response.</returns>
        Task<CRUDResponse> DeleteShift(
            Uri endpoint,
            string jSession,
            string shiftDate,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime);
    }
}