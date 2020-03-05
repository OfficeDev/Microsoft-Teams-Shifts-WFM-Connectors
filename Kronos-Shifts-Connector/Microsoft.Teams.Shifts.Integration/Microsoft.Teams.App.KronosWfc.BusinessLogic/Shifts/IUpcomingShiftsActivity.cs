// <copyright file="IUpcomingShiftsActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using UpcomingShifts = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts;

    /// <summary>
    /// Upcoming shift activity interface.
    /// </summary>
    public interface IUpcomingShiftsActivity
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
    }
}