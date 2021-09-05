// ---------------------------------------------------------------------------
// <copyright file="InMemorySecretsService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public class InMemorySecretsService : ISecretsService
    {
        private CredentialsModel _credentials;

        public Task DeleteCredentialsAsync()
        {
            _credentials = null;
            return Task.CompletedTask;
        }

        public Task<CredentialsModel> GetCredentialsAsync()
        {
            return Task.FromResult(_credentials);
        }

        public Task SaveCredentialsAsync(CredentialsModel value)
        {
            _credentials = value;
            return Task.CompletedTask;
        }
    }
}
