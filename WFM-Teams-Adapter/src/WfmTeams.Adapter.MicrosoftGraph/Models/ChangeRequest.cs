// ---------------------------------------------------------------------------
// <copyright file="ChangeRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using Newtonsoft.Json;

    public class ChangeRequest
    {
        public const string MSPassthroughRequestHeader = "X-MS-WFMPassthrough";
        public const string PassThroughName = "WfmTeams.Adapter.PassThrough";

        [JsonProperty("requests")]
        public ChangeItemRequest[] Requests { get; set; }
    }
}
