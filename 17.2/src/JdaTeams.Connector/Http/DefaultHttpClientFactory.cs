using System.Net.Http;

namespace JdaTeams.Connector.Http
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static HttpClient _httpClient = new HttpClient();

        public HttpClient Client => _httpClient;
        public DelegatingHandler Handler => null;
    }
}
