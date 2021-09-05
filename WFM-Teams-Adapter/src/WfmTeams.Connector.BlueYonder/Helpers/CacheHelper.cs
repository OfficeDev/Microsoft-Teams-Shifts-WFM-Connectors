// ---------------------------------------------------------------------------
// <copyright file="CacheHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WfmTeams.Adapter;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public static class CacheHelper
    {
        public static async Task<List<string>> GetEmployeeIdListAsync(ICacheService cacheService, string teamId)
        {
            var employeeIds = new List<string>();

            // get all the employees for the team
            var teamEmployeeIds = await cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamId).ConfigureAwait(false);
            if (teamEmployeeIds != null)
            {
                foreach (var teamEmployeeId in teamEmployeeIds)
                {
                    var employee = await cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, teamEmployeeId).ConfigureAwait(false);
                    if (employee != null)
                    {
                        employeeIds.Add(employee.WfmEmployeeId);
                    }
                }
            }

            return employeeIds;
        }
    }
}
