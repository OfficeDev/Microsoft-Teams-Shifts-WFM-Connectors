// ---------------------------------------------------------------------------
// <copyright file="InitializeOrchestratorOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    using System;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using WfmTeams.Adapter.Options;

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
