using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.Functions.Options
{
    public class TracingOptions
    {
        public bool TraceEnabled { get; set; }
        public string TraceFolder { get; set; } = "Traces";
        public string TraceIgnore { get; set; } = "vault.azure.net";
    }
}
