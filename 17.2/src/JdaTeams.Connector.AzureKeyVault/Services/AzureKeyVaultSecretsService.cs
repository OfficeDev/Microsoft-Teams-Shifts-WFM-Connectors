using JdaTeams.Connector.AzureKeyVault.Options;
using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace JdaTeams.Connector.AzureKeyVault.Services
{
    public class AzureKeyVaultSecretsService : ISecretsService
    {
        private readonly AzureKeyVaultOptions _options;

        public AzureKeyVaultSecretsService(AzureKeyVaultOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private KeyVaultClient GetKeyVaultClient()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            return new KeyVaultClient((authority, resource, scope) => tokenProvider.KeyVaultTokenCallback(authority, resource, scope));
        }

        private string GetSecretName(string teamId, string provider)
        {
            return $"{teamId}-{provider}";
        }

        private Task<SecretBundle> GetSecretAsync(string teamId, string provider)
        {
            var name = GetSecretName(teamId, provider);
            var client = GetKeyVaultClient();
            return client.GetSecretAsync(_options.KeyVaultConnectionString, name);
        }

        private Task DeleteSecretAsync(string teamId, string provider)
        {
            var name = GetSecretName(teamId, provider);
            var client = GetKeyVaultClient();
            return client.DeleteSecretAsync(_options.KeyVaultConnectionString, name);
        }

        public async Task<CredentialsModel> GetCredentialsAsync(string teamId)
        {
            var secret = await GetSecretAsync(teamId, Providers.Jda);
            var values = new DbConnectionStringBuilder();
            values.ConnectionString = secret.Value;

            var creds = new CredentialsModel();
            creds.Username = values.GetValueOrDefault<string>("username");
            creds.Password = values.GetValueOrDefault<string>("password");
            creds.BaseAddress = values.GetValueOrDefault<string>("baseAddress");
            return creds;
        }

        public async Task<TokenModel> GetTokenAsync(string teamid)
        {
            var secret = await GetSecretAsync(teamid, Providers.Graph);
            var values = new DbConnectionStringBuilder();
            values.ConnectionString = secret.Value;

            var token = new TokenModel();
            token.AccessToken = values.GetValueOrDefault<string>("accessToken");
            token.RefreshToken = values.GetValueOrDefault<string>("refreshToken");
            return token;
        }

        private Task SaveSecretAsync(string teamId, string provider, string value)
        {
            var name = GetSecretName(teamId, provider);
            var client = GetKeyVaultClient();
            return client.SetSecretAsync(_options.KeyVaultConnectionString, name, value);
        }

        public Task SaveCredentialsAsync(string teamId, CredentialsModel value)
        {
            var secret = new DbConnectionStringBuilder();
            secret.Add("username", value.Username);
            secret.Add("password", value.Password);
            secret.Add("baseAddress", value.BaseAddress);
            return SaveSecretAsync(teamId, Providers.Jda, secret.ConnectionString);
        }

        public Task SaveTokenAsync(string teamId, TokenModel value)
        {
            var secret = new DbConnectionStringBuilder();
            secret.Add("accessToken", value.AccessToken);
            secret.Add("refreshToken", value.RefreshToken);
            secret.Add("expiresDate", value.ExpiresDate);
            return SaveSecretAsync(teamId, Providers.Graph, secret.ConnectionString);
        }

        public Task DeleteCredentialsAsync(string teamId)
        {
            return DeleteSecretAsync(teamId, Providers.Jda);
        }

        public Task DeleteTokenAsync(string teamId)
        {
            return DeleteSecretAsync(teamId, Providers.Graph);
        }
    }
}
