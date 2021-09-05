// ---------------------------------------------------------------------------
// <copyright file="OpenShiftRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public abstract class OpenShiftRequestHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate OpenShiftRequestUriTemplate = new UriTemplate("/openshiftrequests/{id}");

        protected readonly ICacheService _cacheService;
        protected readonly IScheduleCacheService _scheduleCacheService;
        protected readonly TeamOrchestratorOptions _teamOptions;
        protected readonly FeatureOptions _featureOptions;
        protected readonly IWfmActionService _wfmActionService;

        protected OpenShiftRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _teamOptions = teamOptions ?? throw new ArgumentNullException(nameof(teamOptions));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmActionService = wfmActionService ?? throw new ArgumentNullException(nameof(wfmActionService));
        }

        protected async Task<ShiftModel> GetOpenShift(string teamId, string openShiftId)
        {
            var loadScheduleTasks = DateTime.UtcNow
                .Range(_teamOptions.PastWeeks, _teamOptions.FutureWeeks, _teamOptions.StartDayOfWeek)
                .Select(w => _scheduleCacheService.LoadScheduleAsync(GetSaveScheduleId(teamId), w));

            var cacheModels = await Task.WhenAll(loadScheduleTasks).ConfigureAwait(false);
            return cacheModels
                .SelectMany(c => c.Tracked)
                .FirstOrDefault(s => s.TeamsShiftId == openShiftId);
        }

        protected static string GetSaveScheduleId(string teamId)
        {
            return teamId + ApplicationConstants.OpenShiftsSuffix;
        }

        protected abstract Task<bool> MapOpenShiftRequestIdentitiesAsync(OpenShiftsChangeRequest openShiftRequest);
    }
}
