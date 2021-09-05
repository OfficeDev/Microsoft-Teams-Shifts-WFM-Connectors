// ---------------------------------------------------------------------------
// <copyright file="SchedulingGroupResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using Newtonsoft.Json;

    public partial class SchedulingGroupResponse
    {
        [JsonProperty("@odata.etag")]
        public string Etag { get; set; }
    }
}
