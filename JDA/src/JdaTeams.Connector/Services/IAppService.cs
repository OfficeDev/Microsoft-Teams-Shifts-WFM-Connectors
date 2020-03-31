using System.IO;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IAppService
    {
        Task<Stream> OpenAppStreamAsync();
    }
}
