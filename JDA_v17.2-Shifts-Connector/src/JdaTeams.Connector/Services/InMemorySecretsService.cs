using JdaTeams.Connector.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public class InMemorySecretsService : ISecretsService
    {
        private readonly ConcurrentDictionary<string, CredentialsModel> _credentials = new ConcurrentDictionary<string, CredentialsModel>();
        private readonly ConcurrentDictionary<string, TokenModel> _tokens = new ConcurrentDictionary<string, TokenModel>();

        public Task<CredentialsModel> GetCredentialsAsync(string teamId)
        {
            if (_credentials.TryGetValue(teamId ?? string.Empty, out var credentialsModel))
            {
                return Task.FromResult(credentialsModel);
            }
            else
            {
                return Task.FromResult(new CredentialsModel());
            }
        }
        public Task SaveCredentialsAsync(string teamId, CredentialsModel value)
        {
            _credentials[teamId ?? string.Empty] = value;

            return Task.CompletedTask;
        }

        public Task DeleteCredentialsAsync(string teamId)
        {
            _credentials.TryRemove(teamId ?? string.Empty, out var credentials);

            return Task.CompletedTask;
        }

        public Task<TokenModel> GetTokenAsync(string teamId)
        {
            if (_tokens.TryGetValue(teamId ?? string.Empty, out var tokenModel))
            {
                return Task.FromResult(tokenModel);
            }
            else
            {
                return Task.FromResult(new TokenModel());
            }
        }

        public Task SaveTokenAsync(string teamId, TokenModel value)
        {
            _tokens[teamId ?? string.Empty] = value;

            return Task.CompletedTask;
        }

        public Task DeleteTokenAsync(string teamId)
        {
            _tokens.TryRemove(teamId ?? string.Empty, out var tokenModel);

            return Task.CompletedTask;
        }
    }
}
