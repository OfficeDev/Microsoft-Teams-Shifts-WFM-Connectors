// ---------------------------------------------------------------------------
// <copyright file="PrepareSyncActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    /// <summary>
    /// Activity that allows the WFM Provider Connector to do whatever work is required to prepare
    /// for the sync that is about to be executed.
    /// </summary>
    public class PrepareSyncActivity
    {
        private readonly IWfmDataService _wfmDataService;

        public PrepareSyncActivity(IWfmDataService wfmDataService)
        {
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
        }

        [FunctionName(nameof(PrepareSyncActivity))]
        public async Task Run([ActivityTrigger] PrepareSyncModel syncModel, ILogger log)
        {
            await _wfmDataService.PrepareSyncAsync(syncModel, log);
        }
    }
}
