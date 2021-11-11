// ---------------------------------------------------------------------------
// <copyright file="CookieHttpHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Flurl;
    using Microsoft.Net.Http.Headers;
    using WfmTeams.Adapter;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Exceptions;
    using WfmTeams.Connector.BlueYonder.Options;

    public class CookieHttpHandler : DelegatingHandler
    {
        protected readonly CredentialsModel _credentials;
        protected readonly BlueYonderPersonaOptions _options;
        protected readonly string _principalId;
        private readonly ICacheService _cacheService;

        public CookieHttpHandler(BlueYonderPersonaOptions options, ICacheService cacheService, CredentialsModel credentials, string principalId)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _principalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
        }

        /// <summary> If the request is to the /users endpoint we need to rewrite the query string
        /// parameters because autorest writes the list of userIds to a comma separated list of
        /// userIds assigned to a single userIds parameter, whereas Blue Yonder is expecting them to be
        /// presented as separate indexed parameters </summary> <param name="request">The request to
        /// rewrite if necessary.</param> <example> /users?userIds=1000191,1000192 will be conveted
        /// to /users?userIds[0]=1000191&usersIds[1]=1000192 </example> <remarks>This method is only
        /// public so that it can be unit tested.</remarks>
        public void RewriteRequestUrl(HttpRequestMessage request)
        {
            const string queryParamName = "userIds";

            var usersEndpoint = _options.RetailWebApiPath.AppendPathSegment("/users").ToString();
            if (request.RequestUri.AbsolutePath.EndsWith(usersEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                var url = new Url(request.RequestUri);
                var userIds = url.QueryParams[queryParamName].ToString().Split(',');
                url = url.RemoveQueryParam(queryParamName);
                for (int i = 0; i < userIds.Length; i++)
                {
                    url = url.SetQueryParam(HttpUtility.UrlEncode($"{queryParamName}[{i}]"), userIds[i]);
                }

                request.RequestUri = url.ToUri();
            }
        }

        protected virtual void AddAuthHeader(HttpRequestMessage request)
        {
            // no implementation in the base class
        }

        protected virtual HttpContent BuildAuthContent()
        {
            var credentials = new Dictionary<string, string>
            {
                { "loginName", _credentials.Username },
                { "password", _credentials.Password }
            };

            return new FormUrlEncodedContent(credentials);
        }

        protected virtual Url BuildAuthEndpoint()
        {
            return _credentials.BaseAddress.AppendPathSegment(_options.BlueYonderCookieAuthPath);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RewriteRequestUrl(request);

            var cookies = await _cacheService.GetKeyAsync<Dictionary<string, string>>(ApplicationConstants.TableNameCookies, _principalId);
            if (cookies == null)
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

            // wait for a short period of time before retrying to allow the Blue Yonder servers time to
            // propagate the cookie
            await Task.Delay(_options.BlueYonderCookieDelayMs);

            response = await base.SendAsync(request, cancellationToken);

            if (IsAuthenticated(response))
            {
                return response;
            }
            else
            {
                throw new BlueYonderUnauthorizedAccessException($"Invalid Blue Yonder access credentials - {nameof(SendAsync)}.", request, response, _credentials.Username, _principalId);
            }
        }

        private void AddCookies(HttpRequestMessage request, Dictionary<string, string> cookies)
        {
            // ensure that the only cookies on the request are the one(s) specifically being added
            if (request.Headers.Contains(HeaderNames.Cookie))
            {
                request.Headers.Remove(HeaderNames.Cookie);
            }

            var values = new List<string>();
            foreach (var key in cookies.Keys)
            {
                values.Add(new Cookie(key, cookies[key]).ToString());
            }

            request.Headers.Add(HeaderNames.Cookie, string.Join(";", values));
        }

        // Because the Blue Yonder API returns 200 whether or not the request was authorized it is necessary
        // to instead get the value of the custom authenticated header that Blue Yonder adds to the response.
        private bool IsAuthenticated(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("authenticated", out var values)
                && bool.TryParse(values.First(), out var authenticated)
                && authenticated;
        }

        private async Task<Dictionary<string, string>> RequestCookiesAsync(CancellationToken cancellationToken)
        {
            var authEndpoint = BuildAuthEndpoint();
            var content = BuildAuthContent();
            var request = new HttpRequestMessage(HttpMethod.Post, authEndpoint)
            {
                Content = content
            };
            AddAuthHeader(request);

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK
                && response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
                && SetCookieHeaderValue.TryParseList(values.ToList(), out var cookies))
            {
                var cookieCollection = new Dictionary<string, string>(cookies.Count);
                foreach (var cookie in cookies)
                {
                    cookieCollection.Add(cookie.Name.Value, cookie.Value.Value);
                }
                await _cacheService.SetKeyAsync(ApplicationConstants.TableNameCookies, _principalId, cookieCollection);

                return cookieCollection;
            }
            else
            {
                throw new BlueYonderUnauthorizedAccessException($"Invalid Blue Yonder access credentials - {nameof(RequestCookiesAsync)}.", request, response, _credentials.Username, _principalId);
            }
        }
    }
}
