using JdaTeams.Connector.Models;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleCacheService
    {
        Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel cacheModel);
        Task<CacheModel> LoadScheduleAsync(string teamId, DateTime weekStartDate);
        Task DeleteScheduleAsync(string teamId, DateTime weekStartDate);
    }
}
