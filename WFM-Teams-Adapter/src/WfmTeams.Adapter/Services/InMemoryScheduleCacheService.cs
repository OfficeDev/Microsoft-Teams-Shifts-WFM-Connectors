// ---------------------------------------------------------------------------
// <copyright file="InMemoryScheduleCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public class InMemoryScheduleCacheService : IScheduleCacheService
    {
        public ConcurrentDictionary<(string, DateTime), CacheModel<ShiftModel>> Schedules { get; set; } = new ConcurrentDictionary<(string, DateTime), CacheModel<ShiftModel>>();

        public Task<string> AcquireLeaseScheduleAsync(string teamId, DateTime weekStartDate, TimeSpan leaseDuration)
        {
            return Task.FromResult("123");
        }

        public Task DeleteScheduleAsync(string teamId, DateTime weekStartDate)
        {
            Schedules.TryRemove((teamId ?? string.Empty, weekStartDate), out var shifts);

            return Task.CompletedTask;
        }

        public Task<CacheModel<ShiftModel>> LoadScheduleAsync(string teamId, DateTime weekStartDate)
        {
            if (Schedules.TryGetValue((teamId ?? string.Empty, weekStartDate), out var shifts))
            {
                return Task.FromResult(shifts);
            }
            else
            {
                return Task.FromResult(new CacheModel<ShiftModel>());
            }
        }

        public Task<LeasedCacheModel<ShiftModel>> LoadScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, TimeSpan leaseTime)
        {
            var shifts = LoadScheduleAsync(teamId, weekStartDate).Result;
            if (shifts != null)
            {
                var leasedShifts = new LeasedCacheModel<ShiftModel>
                {
                    LeaseId = Guid.NewGuid().ToString(),
                    Skipped = shifts.Skipped,
                    Tracked = shifts.Tracked
                };
                return Task.FromResult(leasedShifts);
            }
            else
            {
                return Task.FromResult(new LeasedCacheModel<ShiftModel>());
            }
        }

        public Task ReleaseLeaseScheduleAsync(string teamId, DateTime weekStartDate, string leaseId)
        {
            return Task.CompletedTask;
        }

        public Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel<ShiftModel> cacheModel)
        {
            Schedules[(teamId ?? string.Empty, weekStartDate)] = cacheModel;

            return Task.CompletedTask;
        }

        public Task SaveScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, LeasedCacheModel<ShiftModel> leasedCacheModel)
        {
            return SaveScheduleAsync(teamId, weekStartDate, (CacheModel<ShiftModel>)leasedCacheModel);
        }
    }
}
