using JdaTeams.Connector.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleSourceService
    {
        void SetCredentials(string teamId, CredentialsModel credentials);
        Task<EmployeeModel> GetEmployeeAsync(string teamId, string employeeId);
        Task<StoreModel> GetStoreAsync(string teamId, string storeId);
        Task<JobModel> GetJobAsync(string teamId, string storeId, string jobId);
        Task<List<ShiftModel>> ListWeekShiftsAsync(string teamId, string storeId, DateTime weekStartDate);
        Task LoadEmployeesAsync(string teamId, List<string> employeeIds);
        Task<List<ShiftModel>> ListEmployeeWeekShiftsAsync(string teamId, string employeeId, DateTime weekStartDate);
        Task<List<EmployeeModel>> GetEmployeesAsync(string teamId, string storeId, DateTime weekStartDate);
    }
}
