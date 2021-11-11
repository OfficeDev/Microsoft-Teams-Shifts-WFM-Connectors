// ---------------------------------------------------------------------------
// <copyright file="ScheduleCacheHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Helpers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    internal static class ScheduleCacheHelper
    {
        /// <summary>
        /// Finds the shift with the specified teams shift ID.
        /// </summary>
        /// <param name="cacheModels">The array of cache models.</param>
        /// <param name="shiftId">The Id of the shift to find.</param>
        /// <returns>The shift if found or null.</returns>
        internal static ShiftModel FindShiftByTeamsShiftId(CacheModel<ShiftModel>[] cacheModels, string shiftId)
        {
            return cacheModels
                .SelectMany(c => c.Tracked)
                .FirstOrDefault(s => s.TeamsShiftId == shiftId);
        }

        /// <summary>
        /// Finds the shift with the specified teams shift ID by searching all the cached shifts in
        /// the specified range of weeks.
        /// </summary>
        /// <param name="shiftId">The teams ID of the shift to search for.</param>
        /// <param name="teamId">The ID of the team containing the shift.</param>
        /// <param name="pastWeeks">The number of past weeks to search.</param>
        /// <param name="futureWeeks">The number of future weeks to search.</param>
        /// <param name="startDayOfWeek">The start day of the week for the team.</param>
        /// <param name="scheduleCacheService">The schedule cache service to use to load the schedules.</param>
        /// <param name="timeService">The time service to use to get the current times.</param>
        /// <returns></returns>
        internal static async Task<ShiftModel> FindShiftByTeamsShiftIdAsync(string shiftId, string teamId, int pastWeeks, int futureWeeks, DayOfWeek startDayOfWeek, IScheduleCacheService scheduleCacheService, ISystemTimeService timeService)
        {
            var cacheModels = await LoadSchedulesAsync(teamId, pastWeeks, futureWeeks, startDayOfWeek, scheduleCacheService, timeService).ConfigureAwait(false);
            return FindShiftByTeamsShiftId(cacheModels, shiftId);
        }

        /// <summary>
        /// Loads and returns all the cached schedules for the team between and including the past
        /// and future weeks.
        /// </summary>
        /// <param name="teamId">The ID of the team to return the cached schedules for.</param>
        /// <param name="pastWeeks">The number of past weeks to return.</param>
        /// <param name="futureWeeks">The number of future weeks to return.</param>
        /// <param name="startDayOfWeek">The start day of the week for the team.</param>
        /// <param name="scheduleCacheService">The schedule cache service to use to load the schedules.</param>
        /// <param name="timeService">The time service to use to get the current times.</param>
        /// <returns>The array of cached schedules.</returns>
        internal static async Task<CacheModel<ShiftModel>[]> LoadSchedulesAsync(string teamId, int pastWeeks, int futureWeeks, DayOfWeek startDayOfWeek, IScheduleCacheService scheduleCacheService, ISystemTimeService timeService)
        {
            var loadScheduleTasks = timeService.UtcNow
                .Range(pastWeeks, futureWeeks, startDayOfWeek)
                .Select(w => scheduleCacheService.LoadScheduleAsync(teamId, w));

            return await Task.WhenAll(loadScheduleTasks).ConfigureAwait(false);
        }
    }
}
