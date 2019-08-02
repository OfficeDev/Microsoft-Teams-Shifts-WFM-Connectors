using System.Net.Http;

namespace JdaTeams.Connector.Http
{
    public interface IHttpClientFactory
    {
        HttpClient Client { get; }
        DelegatingHandler Handler { get; }
    }
}
