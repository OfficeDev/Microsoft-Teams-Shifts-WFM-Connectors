using JdaTeams.Connector.Models;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface ISecretsService
    {
        Task<CredentialsModel> GetCredentialsAsync(string teamId);
        Task<TokenModel> GetTokenAsync(string teamId);
        Task SaveCredentialsAsync(string teamId, CredentialsModel value);
        Task DeleteCredentialsAsync(string teamId);
        Task SaveTokenAsync(string teamId, TokenModel value);
        Task DeleteTokenAsync(string teamId);
    }
}
