// ---------------------------------------------------------------------------
// <copyright file="IScheduleCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public interface IScheduleCacheService
    {
        Task DeleteScheduleAsync(string teamId, DateTime weekStartDate);

        Task<CacheModel<ShiftModel>> LoadScheduleAsync(string teamId, DateTime weekStartDate);

        Task<LeasedCacheModel<ShiftModel>> LoadScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, TimeSpan leaseTime);

        Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel<ShiftModel> cacheModel);

        Task SaveScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, LeasedCacheModel<ShiftModel> cacheModel);
    }
}
