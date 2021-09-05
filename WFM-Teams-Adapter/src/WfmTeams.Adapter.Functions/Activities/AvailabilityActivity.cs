// ---------------------------------------------------------------------------
// <copyright file="AvailabilityActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Helpers;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class AvailabilityActivity : DeltaActivity<EmployeeAvailabilityModel>
    {
        public AvailabilityActivity(WeekActivityOptions options, IWfmDataService wfmDataService, ITeamsService teamsService, IDeltaService<EmployeeAvailabilityModel> deltaService, ICacheService cacheService)
            : base(options, wfmDataService, teamsService, deltaService, cacheService)
        {
        }

        [FunctionName(nameof(AvailabilityActivity))]
        public async Task<ResultModel> Run([ActivityTrigger] TeamModel teamModel, ILogger log)
        {
            var activityModel = new TeamActivityModel
            {
                TeamId = teamModel.TeamId,
                ActivityType = "Availability",
                TimeZoneInfoId = teamModel.TimeZoneInfoId
            };

            return await RunDeltaActivity(activityModel, log).ConfigureAwait(false);
        }

        protected override async Task ApplyDeltaAsync(TeamActivityModel activityModel, DeltaModel<EmployeeAvailabilityModel> delta, ILogger log)
        {
            // Teams does not support creating availability items, so we must simply do an update
            // instead of a create
            await UpdateDestinationAsync(nameof(delta.Created), activityModel, delta, delta.Created, _teamsService.UpdateAvailabilityAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Updated), activityModel, delta, delta.Updated, _teamsService.UpdateAvailabilityAsync, log).ConfigureAwait(false);
            await UpdateDestinationAsync(nameof(delta.Deleted), activityModel, delta, delta.Deleted, _teamsService.DeleteAvailabilityAsync, log).ConfigureAwait(false);
        }

        protected override async Task<CacheModel<EmployeeAvailabilityModel>> GetSavedRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            // we are not storing saved records for availability rather we are getting them from the
            // destination (Teams) get all the availability for all employees in the busines unit
            // from Teams api in batches of maximum users
            var teamEmployeeIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, activityModel.TeamId);
            var cacheModel = new CacheModel<EmployeeAvailabilityModel>();
            foreach (var batch in teamEmployeeIds.Buffer(_options.MaximumUsers))
            {
                var tasks = batch.Select(empId => _teamsService.GetEmployeeAvailabilityAsync(empId));
                var result = await Task.WhenAll(tasks).ConfigureAwait(false);

                cacheModel.Tracked.AddRange(result.ToList());

                // wait for the configured interval before processing the next batch
                await Task.Delay(_options.BatchDelayMs).ConfigureAwait(false);
            }

            return cacheModel;
        }

        protected override async Task<List<EmployeeAvailabilityModel>> GetSourceRecordsAsync(TeamActivityModel activityModel, ILogger log)
        {
            var availability = new List<EmployeeAvailabilityModel>();

            List<string> wfmEmployeeIds = await CacheHelper.GetWfmEmployeeIdListAsync(_cacheService, activityModel.TeamId);
            if (wfmEmployeeIds.Count > 0)
            {
                // get the current set of availability records from WFM for all the employees
                availability = await _wfmDataService.ListEmployeeAvailabilityAsync(activityModel.TeamId, wfmEmployeeIds, activityModel.TimeZoneInfoId);
            }
            else
            {
                log.LogInformation($"Awaiting employee cache population for {activityModel.ActivityType} syncronisation.");
            }

            // update with the teams employeeid because this is used as the key in the delta
            foreach (var availabilityItem in availability)
            {
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, availabilityItem.WfmEmployeeId);
                if (employee != null)
                {
                    availabilityItem.TeamsEmployeeId = employee.TeamsEmployeeId;
                }
            }

            return availability;
        }

        protected override void LogRecordError(Exception ex, TeamActivityModel activityModel, string operation, EmployeeAvailabilityModel record, ILogger log)
        {
            log.LogAvailabilityError(ex, activityModel.TeamId, operation, record);
        }

        protected override void LogRecordSkipped(TeamActivityModel activityModel, string operation, EmployeeAvailabilityModel record, ILogger log)
        {
            log.LogAvailabilitySkipped(activityModel.TeamId, operation, record);
        }

        protected override Task SaveRecordsAsync(TeamActivityModel activityModel, CacheModel<EmployeeAvailabilityModel> savedRecords)
        {
            // nothing to do, as we are not saving availability records to intermediate storage
            return Task.CompletedTask;
        }

        protected override async Task SetTeamsIdsAsync(DeltaModel<EmployeeAvailabilityModel> delta, CacheModel<EmployeeAvailabilityModel> savedRecords, TeamActivityModel activityModel, ILogger log)
        {
            // set teams employee ids
            await SetTeamsEmployeeIdsAsync(delta.All.Where(s => string.IsNullOrEmpty(s.TeamsEmployeeId))).ConfigureAwait(false);
        }

        private async Task SetTeamsEmployeeIdsAsync(IEnumerable<EmployeeAvailabilityModel> availabilityRecords)
        {
            foreach (var availabilityRecord in availabilityRecords)
            {
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, availabilityRecord.WfmEmployeeId);
                if (employee != null)
                {
                    availabilityRecord.TeamsEmployeeId = employee.TeamsEmployeeId;
                }
            }
        }
    }
}
