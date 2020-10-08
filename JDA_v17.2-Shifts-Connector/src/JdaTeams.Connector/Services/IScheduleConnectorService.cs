using JdaTeams.Connector.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleConnectorService
    {
        Task<IEnumerable<ConnectionModel>> ListConnectionsAsync();
        Task<ConnectionModel> GetConnectionAsync(string teamId);
        Task SaveConnectionAsync(ConnectionModel model);
        Task DeleteConnectionAsync(string teamId);
        Task<string> GetTimeZoneInfoIdAsync(string TimeZoneName);
    }
}
