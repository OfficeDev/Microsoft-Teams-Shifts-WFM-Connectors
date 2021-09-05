// ---------------------------------------------------------------------------
// <copyright file="ChangeItemRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ChangeItemRequest
    {
        [JsonProperty("body")]
        public JObject Body { get; set; }

        [JsonProperty("headers")]
        public ChangeItemRequestHeaders Headers { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
