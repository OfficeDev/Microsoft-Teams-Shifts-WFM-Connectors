using JdaTeams.Connector.Options;
using Microsoft.Azure.WebJobs;
using System;

namespace JdaTeams.Connector.Functions.Options
{
    public class InitializeOrchestratorOptions : ConnectorOptions
    {
        public bool ClearScheduleEnabled { get; set; } = false;
    }
}
