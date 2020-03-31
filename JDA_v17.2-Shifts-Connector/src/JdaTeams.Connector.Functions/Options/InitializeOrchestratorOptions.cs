using JdaTeams.Connector.Options;
using Microsoft.Azure.WebJobs;
using System;

namespace JdaTeams.Connector.Functions.Options
{
    public class InitializeOrchestratorOptions : ConnectorOptions
    {
        public bool ClearScheduleEnabled { get; set; } = false;

        public RetryOptions AsRetryOptions()
        {
            var retryInterval = TimeSpan.FromSeconds(RetryIntervalSeconds);

            return new RetryOptions(retryInterval, RetryMaxAttempts);
        }

    }
}
