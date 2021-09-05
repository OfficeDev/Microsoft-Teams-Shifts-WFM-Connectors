// ---------------------------------------------------------------------------
// <copyright file="ChangeItemResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using Newtonsoft.Json;

    public class ChangeItemResponse
    {
        [JsonProperty("body")]
        public ChangeItemResponseBody Body { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}
