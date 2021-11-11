// ---------------------------------------------------------------------------
// <copyright file="EmployeeTokenRefreshActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class EmployeeTokenRefreshActivity
    {
        private readonly ICacheService _cacheService;
        private readonly IWfmAuthService _wfmAuthService;

        public EmployeeTokenRefreshActivity(ICacheService cacheService, IWfmAuthService wfmAuthService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmAuthService = wfmAuthService ?? throw new ArgumentNullException(nameof(wfmAuthService));
        }

        [FunctionName(nameof(EmployeeTokenRefreshActivity))]
        public async Task Run([ActivityTrigger] TeamModel teamModel, ILogger log)
        {
            // get the list of team member ids from cache
            var employeeIds = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamModel.TeamId).ConfigureAwait(false);
            if (employeeIds == null)
            {
                // the list has not yet been saved to cache, so exit for now
                return;
            }

            // refresh the employee tokens
            foreach (var employeeId in employeeIds)
            {
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, employeeId).ConfigureAwait(false);
                if (employee == null)
                {
                    // the employee is not in the cache so skip them
                    continue;
                }

                await _wfmAuthService.RefreshEmployeeTokenAsync(employee, teamModel.WfmBuId, log);
            }
        }
    }
}
