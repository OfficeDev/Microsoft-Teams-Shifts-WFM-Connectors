// ---------------------------------------------------------------------------
// <copyright file="ChangeItemDataResponseBody.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ChangeItemDataResponseBody : ChangeItemResponseBody
    {
        [JsonProperty("data")]
        public List<string> Data { get; set; }
    }
}
