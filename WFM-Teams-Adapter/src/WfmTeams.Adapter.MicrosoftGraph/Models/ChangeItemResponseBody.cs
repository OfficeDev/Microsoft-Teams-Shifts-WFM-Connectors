// ---------------------------------------------------------------------------
// <copyright file="ChangeItemResponseBody.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using Newtonsoft.Json;

    public class ChangeItemResponseBody
    {
        [JsonProperty("error")]
        public ChangeErrorResponse Error { get; set; }

        [JsonProperty("eTag")]
        public string Etag { get; set; }
    }
}
