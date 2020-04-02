using System.Net.Http;

namespace JdaTeams.Connector.Http
{
    public class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient Client { get; set; }
        public DelegatingHandler Handler { get; set; }
    }
}
