using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Http
{
    public class MockDelegatingHandler : DelegatingHandler
    {
        private readonly MockHttpHandler _handler;

        public MockDelegatingHandler(MockHttpHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler.Responses.Pop());
        }
    }
}
