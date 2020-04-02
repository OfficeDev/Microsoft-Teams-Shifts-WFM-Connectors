using Flurl;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.Models;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JdaTeams.Connector.JdaPersona.Http
{
    public class CookieHttpHandler : DelegatingHandler
    {
        private static ConcurrentDictionary<string, IList<SetCookieHeaderValue>> _cookieCache = new ConcurrentDictionary<string, IList<SetCookieHeaderValue>>();
        private readonly JdaPersonaOptions _options;
        private readonly CredentialsModel _credentials;
        private readonly string _teamId;

        public CookieHttpHandler(JdaPersonaOptions options, CredentialsModel credentials, string teamId, bool expireToken = false)
        {
            _options = options;
            _credentials = credentials;
            _teamId = teamId;

            if (expireToken)
            {
                ExpireToken();
            }
        }

        public void ExpireToken()
        {
            _cookieCache.TryRemove(_teamId, out var cookie);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RewriteRequestUrl(request);

            if (!_cookieCache.TryGetValue(_teamId, out var cookies) || cookies == null)
            {
                cookies = await RequestCookiesAsync(cancellationToken);
            }

            AddCookies(request, cookies);

            var response = await base.SendAsync(request, cancellationToken);

            if (IsAuthenticated(response))
            {
                return response;
            }

            cookies = await RequestCookiesAsync(cancellationToken);

            AddCookies(request, cookies);

            response = await base.SendAsync(request, cancellationToken);

            if (IsAuthenticated(response))
            {
                return response;
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid JDA access credentials.");
            }
        }

        /// <summary>
        /// If the request is to the /users endpoint we need to rewrite the query string parameters because 
        /// autorest writes the list of userIds to a comma separated list of userIds assigned to a single 
        /// userIds parameter, whereas JDA is expecting them to be presented as separate indexed parameters
        /// </summary>
        /// <param name="request">The request to rewrite if necessary.</param>
        /// <example>
        /// /users?userIds=1000191,1000192 
        /// will be conveted to 
        /// /users?userIds[0]=1000191&usersIds[1]=1000192
        /// </example>
        /// <remarks>This method is only public so that it can be unit tested.</remarks>
        public void RewriteRequestUrl(HttpRequestMessage request)
        {
            const string queryParamName = "userIds";

            var usersEndpoint = _options.JdaApiPath.AppendPathSegment("/users").ToString();
            if(request.RequestUri.AbsolutePath.EndsWith(usersEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                var url = new Url(request.RequestUri);
                var userIds = url.QueryParams[queryParamName].ToString().Split(',');
                url = url.RemoveQueryParam(queryParamName);
                for (int i = 0; i < userIds.Length; i++)
                {
                    url = url.SetQueryParam($"{queryParamName}[{i}]", userIds[i]);
                }

                request.RequestUri = url.ToUri();
            }
        }

        // Because the JDA API returns 200 whether or not the request was authorized it is necessary
        // to instead get the value of the custom authenticated header that JDA adds to the response.
        private bool IsAuthenticated(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("authenticated", out var values)
                && bool.TryParse(values.First(), out var authenticated)
                && authenticated;
        }

        private void AddCookies(HttpRequestMessage request, IList<SetCookieHeaderValue> cookies)
        {
            // ensure that the only cookies on the request are the one(s) specifically being added
            if (request.Headers.Contains(HeaderNames.Cookie))
            {
                request.Headers.Remove(HeaderNames.Cookie);
            }

            var values = cookies
                .Select(c => new Cookie(c.Name.ToString(), c.Value.ToString()).ToString())
                .ToArray();

            request.Headers.Add(HeaderNames.Cookie, string.Join(";", values));
        }

        private async Task<IList<SetCookieHeaderValue>> RequestCookiesAsync(CancellationToken cancellationToken)
        {
            var authEndpoint = _credentials.BaseAddress
                .AppendPathSegment(_options.JdaCookieAuthPath);

            var credentials = new Dictionary<string, string>
            {
                { "loginName", _credentials.Username },
                { "password", _credentials.Password }
            };

            var content = new FormUrlEncodedContent(credentials);
            var request = new HttpRequestMessage(HttpMethod.Post, authEndpoint)
            {
                Content = content
            };
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK 
                && response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
                && SetCookieHeaderValue.TryParseList(values.ToList(), out var cookies))
            {
                _cookieCache[_teamId] = cookies;

                return cookies;
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid JDA access credentials.");
            }
        }
    }
}
