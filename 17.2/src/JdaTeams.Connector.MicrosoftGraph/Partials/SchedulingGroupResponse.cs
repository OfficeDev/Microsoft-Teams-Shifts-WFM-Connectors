using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph.Models
{
    public partial class SchedulingGroupResponse
    {
        [JsonProperty("@odata.etag")]
        public string Etag { get; set; }
    }
}
