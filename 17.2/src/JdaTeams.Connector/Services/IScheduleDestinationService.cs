using JdaTeams.Connector.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleDestinationService
    {
        Task<EmployeeModel> GetEmployeeAsync(string teamId, string login);
        Task CreateShiftAsync(string teamId, ShiftModel shift);
        Task UpdateShiftAsync(string teamId, ShiftModel shift);
        Task DeleteShiftAsync(string teamId, ShiftModel shift);
        Task<List<ShiftModel>> ListShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int maxNumber);
        Task<string> GetSchedulingGroupIdByNameAsync(string teamId, string groupName);
        Task<string> CreateSchedulingGroupAsync(string teamId, string groupName, List<string> userIds);
        Task AddUsersToSchedulingGroupAsync(string teamId, string teamsSchedulingGroupId, List<string> userIds);
        Task<ScheduleModel> GetScheduleAsync(string teamId);
        Task CreateScheduleAsync(string teamId, ScheduleModel schedule);
        Task ShareScheduleAsync(string teamId, DateTime startDate, DateTime endDate, bool notifyTeam);
        Task<List<string>> ListActiveSchedulingGroupIdsAsync(string teamId);
        Task RemoveUsersFromSchedulingGroupAsync(string teamId, string schedulingGroupId);
        Task<GroupModel> GetTeamAsync(string teamId);
    }
}
