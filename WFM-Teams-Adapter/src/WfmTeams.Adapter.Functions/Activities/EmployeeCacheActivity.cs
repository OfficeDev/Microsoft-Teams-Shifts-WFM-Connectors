// ---------------------------------------------------------------------------
// <copyright file="EmployeeCacheActivity.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Mappings;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class EmployeeCacheActivity
    {
        private readonly ICacheService _cacheService;
        private readonly TeamOrchestratorOptions _options;
        private readonly ITeamsService _teamsService;
        private readonly IWfmDataService _wfmDataService;
        private readonly ISecretsService _secretsService;
        private readonly IUserPrincipalMap _userPrincipalMap;

        public EmployeeCacheActivity(TeamOrchestratorOptions options, ICacheService cacheService, IWfmDataService wfmDataService, ITeamsService teamsService, IUserPrincipalMap userPrincipalMap, ISecretsService secretsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            _userPrincipalMap = userPrincipalMap ?? throw new ArgumentNullException(nameof(userPrincipalMap));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        [FunctionName(nameof(EmployeeCacheActivity))]
        public async Task Run([ActivityTrigger] TeamModel teamModel, ILogger log)
        {
            // get the list of team members from Teams
            var teamEmployees = await _teamsService.GetEmployeesAsync(teamModel.TeamId).ConfigureAwait(false);
            var teamEmployeesDict = teamEmployees.ToDictionary(e => e.TeamsLoginName);
            var employeeIds = teamEmployees.Select(e => e.TeamsEmployeeId).ToList();

            await _cacheService.SetKeyAsync(ApplicationConstants.TableNameEmployeeLists, teamModel.TeamId, employeeIds).ConfigureAwait(false);

            // get the list of users from source for each week - this is necessary as the business
            // unit may have different staff members each week
            var weekStartDates = DateTime.UtcNow.Date
                .Range(_options.PastWeeks, _options.FutureWeeks, _options.StartDayOfWeek);

            var weekTasks = weekStartDates
                .Select(weekStartDate => _wfmDataService.GetEmployeesAsync(teamModel.TeamId, teamModel.WfmBuId, weekStartDate));
            var employees = await Task.WhenAll(weekTasks).ConfigureAwait(false);

            // combine the employees from the multiple weeks into a single distinct list
            var sourceEmployees = employees
                .SelectMany(e => e)
                .Distinct(emp => emp.WfmEmployeeId)
                .ToList();

            // merge the data from the source and destination lists in small batches
            var tasks = sourceEmployees.Buffer(10).Select(batch => UpdateCachedEmployeesAsync(batch, teamEmployeesDict, teamModel.TeamId));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // get and save the list of managers for the business unit
            var managerIds = sourceEmployees.Where(e => e.TeamsEmployeeId != null && e.IsManager).Select(e => e.WfmEmployeeId).ToList();
            await _cacheService.SetKeyAsync(ApplicationConstants.TableNameEmployeeLists, teamModel.WfmBuId, managerIds).ConfigureAwait(false);

            log.LogEmployeeCacheActivityInfo(teamModel.TeamId, employeeIds.Count, sourceEmployees.Count, managerIds.Count);
        }

        private async Task UpdateCachedEmployeesAsync(IList<EmployeeModel> sourceEmployees, Dictionary<string, EmployeeModel> teamEmployeesDict, string teamId)
        {
            foreach (var sourceEmployee in sourceEmployees)
            {
                var teamEmployee = _userPrincipalMap.MapEmployee(sourceEmployee.WfmLoginName, teamEmployeesDict);
                if (teamEmployee != null)
                {
                    sourceEmployee.TeamsEmployeeId = teamEmployee.TeamsEmployeeId;
                    sourceEmployee.TeamsLoginName = teamEmployee.TeamsLoginName;

                    // save the employee to cache keyed by both the source and destination ids as we
                    // need to do the lookups from both directions e.g. in weekactivity, the lookup
                    // is from source -> destination e.g. in shift swap, the lookup is from
                    // destination -> source

                    var existing = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, sourceEmployee.TeamsEmployeeId).ConfigureAwait(false);

                    if (existing != null)
                        sourceEmployee.TeamIds = existing.TeamIds;

                    if (!sourceEmployee.TeamIds.Contains(teamId))
                    {
                        sourceEmployee.TeamIds.Add(teamId);
                    }
                    await _cacheService.SetKeyAsync(ApplicationConstants.TableNameEmployees, sourceEmployee.TeamsEmployeeId, sourceEmployee).ConfigureAwait(false);
                    await _cacheService.SetKeyAsync(ApplicationConstants.TableNameEmployees, sourceEmployee.WfmEmployeeId, sourceEmployee).ConfigureAwait(false);
                }
            }
        }
    }
}
