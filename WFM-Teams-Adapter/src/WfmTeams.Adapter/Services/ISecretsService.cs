// ---------------------------------------------------------------------------
// <copyright file="ISecretsService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public interface ISecretsService
    {
        Task DeleteCredentialsAsync();

        Task<CredentialsModel> GetCredentialsAsync();

        Task SaveCredentialsAsync(CredentialsModel value);
    }
}
