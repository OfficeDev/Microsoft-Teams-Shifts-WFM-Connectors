// ---------------------------------------------------------------------------
// <copyright file="IWfmDataService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;

    public interface IWfmDataService
    {
        /// <summary>
        /// Get the set of IDs of eligible shifts that the specified shift is allowed to swap with.
        /// </summary>
        /// <param name="shift">The shift to get the eligible swap targets for.</param>
        /// <param name="employee">The model containing the employee data.</param>
        /// <param name="buId">The ID of the WFM business unit.</param>
        /// <returns>The set of ID's of eligible shift swap targets, if any.</returns>
        Task<List<string>> GetEligibleTargetsForShiftSwap(ShiftModel shift, EmployeeModel employee, string buId);

        Task<List<string>> GetDepartmentsAsync(string teamId, string wfmBuId);

        Task<List<EmployeeModel>> GetEmployeesAsync(string teamId, string wfmBuId, DateTime weekStartDate);

        Task<JobModel> GetJobAsync(string teamId, string wfmBuId, string jobId);

        Task<BusinessUnitModel> GetBusinessUnitAsync(string buId, ILogger log);

        Task<List<EmployeeAvailabilityModel>> ListEmployeeAvailabilityAsync(string teamId, List<string> employeeIds, string timeZoneInfoId);

        Task<List<ShiftModel>> ListWeekShiftsAsync(string teamId, string wfmBuId, DateTime weekStartDate, string timeZoneInfoId);

        Task<List<ShiftModel>> ListWeekOpenShiftsAsync(string teamId, string wfmBuId, DateTime weekStartDate, string timeZoneInfoId, EmployeeModel wfmBuManager);

        Task<List<TimeOffModel>> ListWeekTimeOffAsync(string teamId, List<string> employeeIds, DateTime weekStartDate, string timeZoneInfoId);

        Task<bool> PrepareSyncAsync(PrepareSyncModel syncModel, ILogger log);
    }
}
