// ---------------------------------------------------------------------------
// <copyright file="DeltaActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public abstract class DeltaActivity<T> where T : IDeltaItem
    {
        protected readonly ICacheService _cacheService;
        protected readonly IDeltaService<T> _deltaService;
        protected readonly WeekActivityOptions _options;
        protected readonly ITeamsService _teamsService;
        protected readonly IWfmDataService _wfmDataService;

        protected DeltaActivity(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<T> deltaService, ICacheService cacheService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            _deltaService = deltaService ?? throw new ArgumentNullException(nameof(deltaService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        protected abstract Task ApplyDeltaAsync(TeamActivityModel activityModel, DeltaModel<T> delta, ILogger log);

        protected abstract Task<CacheModel<T>> GetSavedRecordsAsync(TeamActivityModel activityModel, ILogger log);

        protected abstract Task<List<T>> GetSourceRecordsAsync(TeamActivityModel activityModel, ILogger log);

        protected abstract void LogRecordError(Exception ex, TeamActivityModel activityModel, string operation, T record, ILogger log);

        protected abstract void LogRecordSkipped(TeamActivityModel activityModel, string operation, T record, ILogger log);

        protected async Task<ResultModel> RunDeltaActivity(TeamActivityModel activityModel, ILogger log)
        {
            log.LogActivity(activityModel);

            /*
                before we do anything at all for the activity we should ensure that the cache is populated for this
                team because if it is not then we will either end up with:
                1. a large number of errors and/or;
                2. a larger number of skipped items or;
                3. a computed delta of all deletes where all records in the period are deleted in Teams
            */
            var teamEmployeeIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, activityModel.TeamId).ConfigureAwait(false);
            if (teamEmployeeIds == null || teamEmployeeIds.Count == 0)
            {
                log.LogActivitySkipped(activityModel, "Employee cache not populated.");
                return new ResultModel();
            }

            var sourceRecords = await GetSourceRecordsAsync(activityModel, log).ConfigureAwait(false);
            log.LogSourceRecords(sourceRecords.Count, activityModel);
            if (sourceRecords.Count == 0 && _options.AbortSyncOnZeroSourceRecords)
            {
                log.LogActivitySkipped(activityModel, "Zero source records returned.");
                return new ResultModel();
            }

            var savedRecords = await GetSavedRecordsAsync(activityModel, log).ConfigureAwait(false);

            // compute the delta
            var delta = _deltaService.ComputeDelta(savedRecords.Tracked, sourceRecords);
            log.LogFullDelta(activityModel.TeamId, activityModel.DateValue, delta, activityModel.ActivityType);

            if (delta.HasChanges)
            {
                delta.RemoveSkipped(savedRecords.Skipped);
                delta.ApplyMaximum(_options.MaximumDelta);

                log.LogPartialDelta(activityModel.TeamId, activityModel.DateValue, delta, activityModel.ActivityType);

                await SetTeamsIdsAsync(delta, savedRecords, activityModel, log).ConfigureAwait(false);

                // update teams
                await ApplyDeltaAsync(activityModel, delta, log).ConfigureAwait(false);

                log.LogAppliedDelta(activityModel.TeamId, activityModel.DateValue, delta, activityModel.ActivityType);

                await UpdateSavedRecordsAsync(activityModel, savedRecords, delta).ConfigureAwait(false);
            }

            return delta.AsResult();
        }

        protected abstract Task SaveRecordsAsync(TeamActivityModel activityModel, CacheModel<T> savedRecords);

        protected virtual async Task SetTeamsEmployeeIdsAsync<E>(IEnumerable<E> items, string teamId) where E : IDeltaItem
        {
            var teamEmployeeIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamId).ConfigureAwait(false);

            if (teamEmployeeIds?.Count > 0)
            {
                foreach (var item in items)
                {
                    var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, item.WfmEmployeeId).ConfigureAwait(false);
                    if (employee != null && teamEmployeeIds.Contains(employee.TeamsEmployeeId))
                    {
                        item.TeamsEmployeeId = employee.TeamsEmployeeId;
                    }
                }
            }
        }

        protected abstract Task SetTeamsIdsAsync(DeltaModel<T> delta, CacheModel<T> savedRecords, TeamActivityModel activityModel, ILogger log);

        protected async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, IEnumerable<T> records, Func<T, Task> destinationMethod, ILogger log)
        {
            var tasks = records
                .Select(record => UpdateDestinationAsync(operation, activityModel, delta, record, destinationMethod, log))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        protected async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, IEnumerable<T> records, Func<string, T, Task> destinationMethod, ILogger log)
        {
            var tasks = records
                .Select(record => UpdateDestinationAsync(operation, activityModel, delta, record, destinationMethod, log))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        protected async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, IEnumerable<T> records, Func<string, T, bool, Task> destinationMethod, ILogger log)
        {
            var tasks = records
                .Select(record => UpdateDestinationAsync(operation, activityModel, delta, record, destinationMethod, log))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        protected virtual async Task UpdateSavedRecordsAsync(TeamActivityModel activityModel, CacheModel<T> savedRecords, DeltaModel<T> delta)
        {
            delta.ApplyChanges(savedRecords.Tracked);
            delta.ApplySkipped(savedRecords.Skipped);
            await SaveRecordsAsync(activityModel, savedRecords).ConfigureAwait(false);
        }

        private async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, T record, Func<T, Task> destinationMethod, ILogger log)
        {
            try
            {
                await destinationMethod(record).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                delta.SkippedChange(record);
                LogRecordSkipped(activityModel, operation, record, log);
            }
            catch (Exception ex)
            {
                delta.FailedChange(record);
                LogRecordError(ex, activityModel, operation, record, log);
            }
        }

        private async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, T record, Func<string, T, Task> destinationMethod, ILogger log)
        {
            try
            {
                await destinationMethod(activityModel.TeamId, record).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                delta.SkippedChange(record);
                LogRecordSkipped(activityModel, operation, record, log);
            }
            catch (Exception ex)
            {
                delta.FailedChange(record);
                LogRecordError(ex, activityModel, operation, record, log);
            }
        }

        private async Task UpdateDestinationAsync(string operation, TeamActivityModel activityModel, DeltaModel<T> delta, T record, Func<string, T, bool, Task> destinationMethod, ILogger log)
        {
            try
            {
                await destinationMethod(activityModel.TeamId, record, false).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                delta.SkippedChange(record);
                LogRecordSkipped(activityModel, operation, record, log);
            }
            catch (Exception ex)
            {
                delta.FailedChange(record);
                LogRecordError(ex, activityModel, operation, record, log);
            }
        }
    }
}
