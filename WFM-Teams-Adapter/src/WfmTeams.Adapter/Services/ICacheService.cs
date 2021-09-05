// ---------------------------------------------------------------------------
// <copyright file="ICacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;

    public interface ICacheService
    {
        Task DeleteKeyAsync(string tableName, string id);

        Task<T> GetKeyAsync<T>(string tableName, string id);

        Task SetKeyAsync<T>(string tableName, string id, T value, bool shortExpiry = false);
    }
}
