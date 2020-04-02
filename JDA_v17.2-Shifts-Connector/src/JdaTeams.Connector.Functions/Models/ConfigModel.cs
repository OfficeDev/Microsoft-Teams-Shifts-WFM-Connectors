using Newtonsoft.Json;

namespace JdaTeams.Connector.Functions.Models
{
    public class ConfigModel
    {
        public string AuthorizeUrl { get; set; }
        public string ClientId { get; set; }
        public string Scope { get; set; }
        public string JdaBaseAddress { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? Connected { get; set; }
        public string ShiftsAppUrl { get; set; }
    }
}
