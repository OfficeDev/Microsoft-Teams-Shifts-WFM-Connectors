using System;
using JdaTeams.Connector.Options;
using Microsoft.Azure.WebJobs;

namespace JdaTeams.Connector.Functions.Options
{
    public class TeamOrchestratorOptions : ConnectorOptions
    {
        public int FrequencySeconds { get; set; } = 60 * 15;
        public bool ContinueOnError { get; set; } = true;
        public int PastWeeks { get; set; } = 3;
        public int FutureWeeks { get; set; } = 3;

        public RetryOptions AsRetryOptions()
        {
            var retryInterval = TimeSpan.FromSeconds(RetryIntervalSeconds);

            return new RetryOptions(retryInterval, RetryMaxAttempts);
        }
    }
}
