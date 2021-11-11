// ---------------------------------------------------------------------------
// <copyright file="ITeamsService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public interface ITeamsService
    {
        Task AddUsersToSchedulingGroupAsync(string teamId, string teamsSchedulingGroupId, List<string> userIds);

        Task ApproveOpenShiftRequest(string requestId, string teamId);

        Task ApproveSwapShiftsRequest(string message, string requestId, string teamId);

        Task CreateOpenShiftAsync(string teamId, ShiftModel shift);

        Task CreateScheduleAsync(string teamId, ScheduleModel schedule);

        Task<string> CreateSchedulingGroupAsync(string teamId, string groupName, List<string> userIds);

        Task CreateShiftAsync(string teamId, ShiftModel shift);

        Task CreateTimeOffAsync(string teamId, TimeOffModel timeOff);

        Task<TimeOffReasonModel> CreateTimeOffReasonAsync(string teamId, TimeOffReasonModel timeOffType);

        Task DeclineOpenShiftRequest(string requestId, string teamId, string message);

        Task DeleteAvailabilityAsync(EmployeeAvailabilityModel availability);

        Task DeleteOpenShiftAsync(string teamId, ShiftModel shift, bool draftDelete);

        Task DeleteShiftAsync(string teamId, ShiftModel shift, bool draftDelete);

        Task DeleteTimeOffAsync(string teamId, TimeOffModel timeOff, bool draftDelete);

        Task<EmployeeAvailabilityModel> GetEmployeeAvailabilityAsync(string userId);

        Task<List<EmployeeModel>> GetEmployeesAsync(string teamId);

        Task<ScheduleModel> GetScheduleAsync(string teamId);

        Task<string> GetSchedulingGroupIdByNameAsync(string teamId, string groupName);

        Task<List<SchedulingGroupModel>> GetSchedulingGroupsAsync(string teamId);

        Task<GroupModel> GetTeamAsync(string teamId);

        Task<List<string>> ListActiveSchedulingGroupIdsAsync(string teamId);

        Task<List<ShiftModel>> ListOpenShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize);

        Task<List<ShiftModel>> ListShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize);

        Task<List<TimeOffModel>> ListTimeOffAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize);

        Task<List<TimeOffReasonModel>> ListTimeOffReasonsAsync(string teamId);

        Task RemoveUsersFromSchedulingGroupAsync(string teamId, string schedulingGroupId);

        Task ShareScheduleAsync(string teamId, DateTime startDate, DateTime endDate, bool notifyTeam);

        Task UpdateAvailabilityAsync(EmployeeAvailabilityModel availability);

        Task UpdateOpenShiftAsync(string teamId, ShiftModel shift);

        Task UpdateScheduleAsync(string teamId, ScheduleModel schedule);

        Task UpdateShiftAsync(string teamId, ShiftModel shift);

        Task UpdateTimeOffAsync(string teamId, TimeOffModel timeOff);
    }
}
