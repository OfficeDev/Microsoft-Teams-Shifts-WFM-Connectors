// ---------------------------------------------------------------------------
// <copyright file="ListNextPageMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public partial interface IMicrosoftGraphClient
    {
        Task<HttpOperationResponse<T>> ListNextPageWithHttpMessagesAsync<T>(string url, CancellationToken cancellationToken = default);
    }

    public partial class MicrosoftGraphClientExtensions
    {
        public static ShiftCollectionResponse ListShiftsNextPage(this IMicrosoftGraphClient operations, string url)
        {
            return operations.ListShiftsNextPageAsync(url).GetAwaiter().GetResult();
        }

        public static async Task<ShiftCollectionResponse> ListShiftsNextPageAsync(this IMicrosoftGraphClient operations, string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var _result = await operations.ListNextPageWithHttpMessagesAsync<ShiftCollectionResponse>(url, cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }

        public static OpenShiftCollectionResponse ListOpenShiftsNextPage(this IMicrosoftGraphClient operations, string url)
        {
            return operations.ListOpenShiftsNextPageAsync(url).GetAwaiter().GetResult();
        }

        public static async Task<OpenShiftCollectionResponse> ListOpenShiftsNextPageAsync(this IMicrosoftGraphClient operations, string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var _result = await operations.ListNextPageWithHttpMessagesAsync<OpenShiftCollectionResponse>(url, cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }

        public static TimeOffCollectionResponse ListTimeOffNextPage(this IMicrosoftGraphClient operations, string url)
        {
            return operations.ListTimeOffNextPageAsync(url).GetAwaiter().GetResult();
        }

        public static async Task<TimeOffCollectionResponse> ListTimeOffNextPageAsync(this IMicrosoftGraphClient operations, string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var _result = await operations.ListNextPageWithHttpMessagesAsync<TimeOffCollectionResponse>(url, cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }
    }

    public partial class MicrosoftGraphClient
    {
        public async Task<HttpOperationResponse<T>> ListNextPageWithHttpMessagesAsync<T>(string url, CancellationToken cancellationToken)
        {
            var _httpRequest = new HttpRequestMessage();
            _httpRequest.Method = new HttpMethod("GET");
            _httpRequest.RequestUri = new System.Uri(url);

            var _httpResponse = await HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            string _responseContent = null;

            if ((int)_statusCode != 200)
            {
                var ex = new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", _statusCode));
                if (_httpResponse.Content != null)
                {
                    _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    _responseContent = string.Empty;
                }
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                _httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<T>();
            _result.Request = _httpRequest;
            _result.Response = _httpResponse;
            // Deserialize Response
            if ((int)_statusCode == 200)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = SafeJsonConvert.DeserializeObject<T>(_responseContent, DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }

            return _result;
        }
    }
}

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public partial class ShiftCollectionResponse
    {
        public ShiftCollectionResponse(IList<ShiftResponse> value = default(IList<ShiftResponse>), string oDataNextLink = default(string))
        {
            Value = value;
            ODataNextLink = oDataNextLink;
        }

        [JsonProperty(PropertyName = "@odata.nextlink")]
        public string ODataNextLink { get; set; }
    }

    public partial class OpenShiftCollectionResponse
    {
        public OpenShiftCollectionResponse(IList<OpenShiftResponse> value = default(IList<OpenShiftResponse>), string oDataNextLink = default(string))
        {
            Value = value;
            ODataNextLink = oDataNextLink;
        }

        [JsonProperty(PropertyName = "@odata.nextlink")]
        public string ODataNextLink { get; set; }
    }

    public partial class TimeOffCollectionResponse
    {
        public TimeOffCollectionResponse(IList<TimeOffResponse> value = default(IList<TimeOffResponse>), string oDataNextLink = default(string))
        {
            Value = value;
            ODataNextLink = oDataNextLink;
        }

        [JsonProperty(PropertyName = "@odata.nextlink")]
        public string ODataNextLink { get; set; }
    }
}
