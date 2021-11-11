// ---------------------------------------------------------------------------
// <copyright file="AzureKeyVaultSecretsService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureKeyVault.Services
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using WfmTeams.Adapter.AzureKeyVault.Options;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class AzureKeyVaultSecretsService : ISecretsService
    {
        private readonly AzureKeyVaultOptions _options;
        private const string CredentialSecretName = "wfmcreds";

        public AzureKeyVaultSecretsService(AzureKeyVaultOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task DeleteCredentialsAsync()
        {
            return DeleteSecretAsync(CredentialSecretName);
        }

        public async Task<CredentialsModel> GetCredentialsAsync()
        {
            var secret = await GetSecretAsync(CredentialSecretName).ConfigureAwait(false);
            var values = new DbConnectionStringBuilder
            {
                ConnectionString = secret.Value
            };

            return new CredentialsModel
            {
                Username = values.ReplGetValueOrDefault<string>("username"),
                Password = values.ReplGetValueOrDefault<string>("password")
            };
        }

        public Task SaveCredentialsAsync(CredentialsModel value)
        {
            var secret = new DbConnectionStringBuilder
            {
                { "username", value.Username },
                { "password", value.Password }
            };
            return SaveSecretAsync(CredentialSecretName, secret.ConnectionString);
        }

        private async Task DeleteSecretAsync(string secretName)
        {
            var client = GetKeyVaultClient();
            await client.StartDeleteSecretAsync(secretName).ConfigureAwait(false);
        }

        private SecretClient GetKeyVaultClient()
        {
            return new SecretClient(new Uri(_options.KeyVaultConnectionString), new DefaultAzureCredential());
        }

        private async Task<KeyVaultSecret> GetSecretAsync(string secretName)
        {
            var client = GetKeyVaultClient();
            return await client.GetSecretAsync(secretName).ConfigureAwait(false);
        }

        private async Task SaveSecretAsync(string secretName, string value)
        {
            var client = GetKeyVaultClient();
            await client.SetSecretAsync(secretName, value).ConfigureAwait(false);
        }
    }
}
