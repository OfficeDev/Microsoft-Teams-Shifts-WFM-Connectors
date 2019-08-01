using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Http
{
    public class MockHttpHandler : HttpMessageHandler
    {
        public Stack<HttpResponseMessage> Responses { get; set; } = new Stack<HttpResponseMessage>();

        public MockHttpHandler()
        {

        }

        public MockHttpHandler(HttpStatusCode statusCode, string content = "")
        {
            WithResponseMessage(statusCode, content);
        }

        public MockHttpHandler WithResponseMessage(HttpStatusCode statusCode, string content = "", string mediaType = "application/json")
        {
            Responses.Push(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, mediaType)
            });

            return this;
        }

        public MockHttpHandler WithResponseMessage(HttpStatusCode statusCode, object value)
        {
            var content = JsonConvert.SerializeObject(value);

            WithResponseMessage(statusCode, content);

            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Responses.Pop());
        }

        public IHttpClientFactory BuildClientFactory()
        {
            return new MockHttpClientFactory
            {
                Client = new HttpClient(this),
                Handler = new MockDelegatingHandler(this)
            };
        }
    }
}
