using JdaTeams.Connector.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public class InMemoryScheduleCacheService : IScheduleCacheService
    {
        public ConcurrentDictionary<(string, DateTime), CacheModel> Schedules { get; set; } = new ConcurrentDictionary<(string, DateTime), CacheModel>();

        public Task<CacheModel> LoadScheduleAsync(string teamId, DateTime weekStartDate)
        {
            if (Schedules.TryGetValue((teamId ?? string.Empty, weekStartDate), out var shifts))
            {
                return Task.FromResult(shifts);
            }
            else
            {
                return Task.FromResult(new CacheModel());
            }
        }

        public Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel cacheModel)
        {
            Schedules[(teamId ?? string.Empty, weekStartDate)] = cacheModel;

            return Task.CompletedTask;
        }

        public Task DeleteScheduleAsync(string teamId, DateTime weekStartDate)
        {
            Schedules.TryRemove((teamId ?? string.Empty, weekStartDate), out var shifts);

            return Task.CompletedTask;
        }
    }
}
