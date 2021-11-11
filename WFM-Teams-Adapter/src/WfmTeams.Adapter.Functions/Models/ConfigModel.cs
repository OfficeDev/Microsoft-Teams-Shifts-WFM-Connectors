// ---------------------------------------------------------------------------
// <copyright file="ConfigModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using Newtonsoft.Json;

    public class ConfigModel
    {
        public string AuthorizeUrl { get; set; }
        public string ClientId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? Connected { get; set; }

        public string Scope { get; set; }
        public string ShiftsAppUrl { get; set; }
    }
}
