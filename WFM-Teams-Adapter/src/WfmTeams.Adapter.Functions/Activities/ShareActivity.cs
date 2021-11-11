// ---------------------------------------------------------------------------
// <copyright file="ShareActivity.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Services;

    public class ShareActivity
    {
        private readonly WeekActivityOptions _options;

        private readonly ITeamsService _teamsService;

        public ShareActivity(WeekActivityOptions options, ITeamsService teamsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ShareActivity))]
        public async Task Run([ActivityTrigger] ShareModel shareModel, ILogger log)
        {
            await _teamsService.ShareScheduleAsync(shareModel.TeamId, shareModel.StartDate, shareModel.EndDate, _options.NotifyTeamOnChange).ConfigureAwait(false);
            log.LogShareSchedule(shareModel.StartDate, shareModel.EndDate, _options.NotifyTeamOnChange, shareModel.TeamId);
        }
    }
}
