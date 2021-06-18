// <copyright file="ISwapShiftEligibilityActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShiftEligibility
{
    using System;
    using System.Threading.Tasks;
    using Response = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShiftEligibility.Response;

    /// <summary>
    /// The Swap Shift Eligibility Activity interface.
    /// </summary>
    public interface ISwapShiftEligibilityActivity
    {
        /// <summary>
        /// Sends the swap eligibility request.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="startTime">The start time for the requestor's shift.</param>
        /// <param name="endTime">The end time for the requestor's shift.</param>
        /// <param name="queryDate">The date for the requestor's shift.</param>
        /// <param name="shiftSwapDate">The date time for the potential requested shift.</param>
        /// <param name="employeeNumber">The employee number of the requestor.</param>
        /// <returns>Response object.</returns>
        Task<Response> SendEligibilityRequestAsync(
            Uri endPointUrl,
            string jSession,
            string startTime,
            string endTime,
            string queryDate,
            string shiftSwapDate,
            string employeeNumber);

        /// <summary>
        /// Creates the swap eligibility request.
        /// </summary>
        /// <param name="startTime">The start time for the requestor's shift.</param>
        /// <param name="endTime">The end time for the requestor's shift.</param>
        /// <param name="queryDate">The date for the requestor's shift.</param>
        /// <param name="shiftSwapDate">The date time for the potential requested shift.</param>
        /// <param name="employeeNumber">The employee number of the requestor.</param>
        /// <returns>The XML request as a string.</returns>
        string CreateEligibilityRequest(
            string startTime,
            string endTime,
            string queryDate,
            string shiftSwapDate,
            string employeeNumber);
    }
}
