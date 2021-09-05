// ---------------------------------------------------------------------------
// <copyright file="ITimeOffCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public interface ITimeOffCacheService
    {
        Task DeleteTimeOffAsync(string teamId, DateTime weekStartDate);

        Task<CacheModel<TimeOffModel>> LoadTimeOffAsync(string teamId, DateTime weekStartDate);

        Task SaveTimeOffAsync(string teamId, DateTime weekStartDate, CacheModel<TimeOffModel> cacheModel);
    }
}
