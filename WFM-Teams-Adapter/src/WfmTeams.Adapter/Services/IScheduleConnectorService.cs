// ---------------------------------------------------------------------------
// <copyright file="IScheduleConnectorService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public interface IScheduleConnectorService
    {
        Task DeleteConnectionAsync(string teamId);

        Task<ConnectionModel> GetConnectionAsync(string teamId);

        Task<IEnumerable<ConnectionModel>> ListConnectionsAsync();

        Task SaveConnectionAsync(ConnectionModel model);

        Task UpdateEnabledAsync(string teamId, bool enabled);

        Task UpdateLastExecutionDatesAsync(ConnectionModel model);
    }
}
