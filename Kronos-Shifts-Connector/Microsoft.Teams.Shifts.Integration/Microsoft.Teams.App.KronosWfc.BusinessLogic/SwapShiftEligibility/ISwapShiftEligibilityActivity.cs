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
        /// <param name="offeredStartTime">The start time for the requestor's shift.</param>
        /// <param name="offeredEndTime">The end time for the requestor's shift.</param>
        /// <param name="offeredShiftDate">The date for the requestor's shift.</param>
        /// <param name="requestedShiftDate">The date time for the potential requested shift.</param>
        /// <param name="employeeNumber">The employee number of the requestor.</param>
        /// <returns>Response object.</returns>
        Task<Response> SendEligibilityRequestAsync(
            Uri endPointUrl,
            string jSession,
            string offeredStartTime,
            string offeredEndTime,
            string offeredShiftDate,
            string requestedShiftDate,
            string employeeNumber);
    }
}
