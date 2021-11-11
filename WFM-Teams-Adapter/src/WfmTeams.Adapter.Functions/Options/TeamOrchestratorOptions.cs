// ---------------------------------------------------------------------------
// <copyright file="TeamOrchestratorOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    using System;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using WfmTeams.Adapter.Options;

    public class TeamOrchestratorOptions : ConnectorOptions
    {
        public int AvailabilityFrequencyMinutes { get; set; } = 1440;
        public int ChangeRequestWaitSeconds { get; set; } = 3;
        public bool ContinueOnError { get; set; } = true;
        public int DelayedActionSeconds { get; set; } = 5;
        public int EmployeeCacheFrequencyMinutes { get; set; } = 30;
        public int EmployeeTokenRefreshBatchSize { get; set; } = 10;
        public int EmployeeTokenRefreshFrequencyMinutes { get; set; } = 31;
        public int FutureWeeks { get; set; } = 3;
        public int MaximumTeams { get; set; } = 2;
        public int OpenShiftsFrequencyMinutes { get; set; } = 15;
        public int OrchestratorHungThresholdMinutes { get; set; } = 60;
        public int PastWeeks { get; set; } = 3;
        public int ShiftsFrequencyMinutes { get; set; } = 15;
        public bool SuspendAllSyncs { get; set; }
        public int TimeOffFrequencyMinutes { get; set; } = 60;

        public RetryOptions AsRetryOptions()
        {
            var retryInterval = TimeSpan.FromSeconds(RetryIntervalSeconds);

            return new RetryOptions(retryInterval, RetryMaxAttempts);
        }
    }
}
