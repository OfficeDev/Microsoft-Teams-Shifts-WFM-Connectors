using JdaTeams.Connector.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleConnectorService
    {
        Task DeleteConnectionAsync(string teamId);

        Task<ConnectionModel> GetConnectionAsync(string teamId);

        Task<IEnumerable<ConnectionModel>> ListConnectionsAsync();

        Task SaveConnectionAsync(ConnectionModel model);
    }
}