using Flurl;
using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Http;
using JdaTeams.Connector.JdaPersona.Http;
using JdaTeams.Connector.JdaPersona.Models;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JdaTeams.Connector.JdaPersona.Services
{
    public class JdaPersonaScheduleSourceService : IScheduleSourceService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, JobModel>> _teamJobs = new ConcurrentDictionary<string, ConcurrentDictionary<string, JobModel>>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _teamDepartments = new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EmployeeModel>> _teamEmployees = new ConcurrentDictionary<string, ConcurrentDictionary<string, EmployeeModel>>();
        private readonly ConcurrentDictionary<string, CredentialsModel> _credentials = new ConcurrentDictionary<string, CredentialsModel>();

        private readonly JdaPersonaOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public JdaPersonaScheduleSourceService(JdaPersonaOptions options, IHttpClientFactory httpClientFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public void SetCredentials(string teamId, CredentialsModel credentials)
        {
            _credentials[teamId] = credentials;
        }

        public async Task<string> GetJdaTimeZoneNameAsync(string teamId, int TimeZoneId)
        {
            var jdaPersonaClient = CreateClient(teamId);

            var timeZone = await jdaPersonaClient.GetTimeZoneByIdAsync(TimeZoneId);

            return timeZone.Name;
        }

        public async Task LoadEmployeesAsync(string teamId, List<string> employeeIds)
        {
            var jdaPersonaClient = CreateClient(teamId);

            // get all the users from the JDA api in batches of maximum users
            var tasks = employeeIds.Buffer(_options.MaximumUsers).Select(batch => jdaPersonaClient.GetUsersAsync(batch)).ToArray();
            await Task.WhenAll(tasks);

            var employees = tasks
                .SelectMany(t => t.Result)
                .Select(jdaUser => new EmployeeModel
                {
                    SourceId = jdaUser.UserID.ToString(),
                    LoginName = jdaUser.Name?.LoginName,
                    DisplayName = jdaUser.Name?.DisplayName
                })
                .ToDictionary(u => u.SourceId, u => u);

            _teamEmployees[teamId] = new ConcurrentDictionary<string, EmployeeModel>(employees);
        }

        public async Task<EmployeeModel> GetEmployeeAsync(string teamId, string employeeId)
        {
            if(!_teamEmployees.TryGetValue(teamId, out var employees))
            {
                throw new NotSupportedException($"{nameof(LoadEmployeesAsync)} must be called before this method can be used.");
            }

            return await Task.FromResult(employees.GetValueOrDefault(employeeId));
        }

        public async Task<StoreModel> GetStoreAsync(string teamId, string storeId)
        {
            var siteId = Guard.ArgumentIsInteger(storeId, nameof(storeId));
            var jdaPersonaClient = CreateClient(teamId, expireToken: true);

            try
            {
                var jdaSite = await jdaPersonaClient.GetSiteByIdAsync(siteId);

                return new StoreModel
                {
                    StoreId = jdaSite.Id.ToString(),
                    StoreName = jdaSite.Name,
                    TimeZoneId = jdaSite.TimeZoneAssignmentID
                };
            }
            catch (HttpOperationException hex) when (hex.Response?.ReasonPhrase == "Not Found")
            {
                throw new KeyNotFoundException($"Store with ID {storeId} was not found.");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Failed to get the store from JDA", ex);
            }
        }

        public async Task<List<ShiftModel>> ListWeekShiftsAsync(string teamId, string storeId, DateTime weekStartDate, string TimeZoneInfoId)
        {
            var jdaPersonaClient = CreateClient(teamId);
            var siteId = int.Parse(storeId);
            var weekShifts = await jdaPersonaClient.GetSiteShiftsForWeekAsync(siteId, weekStartDate);

            return MapShifts(weekShifts, TimeZoneInfoId);
        }

        public async Task<JobModel> GetJobAsync(string teamId, string storeId, string jobId)
        {
            if(!_teamJobs.ContainsKey(teamId))
            {
                await LoadHierarchyAsync(teamId, storeId);
            }

            return await Task.FromResult(_teamJobs[teamId].GetValueOrDefault(jobId));
         }

        private JdaPersonaClient CreateClient(string teamId, bool expireToken = false)
        {
            var credentials = _credentials.GetValueOrDefault(teamId) ?? _options.AsCredentials();
            var httpHandler = _httpClientFactory.Handler ?? new CookieHttpHandler(_options, credentials, teamId, expireToken);
            var apiBaseAddress = credentials.BaseAddress
                .AppendPathSegment(_options.JdaApiPath);

            return new JdaPersonaClient(new Uri(apiBaseAddress), httpHandler);
        }

        private DateTime ConvertFromLocalTime(DateTime dateTime, string TimeZoneInfoId)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfoId);
            var localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);
        }

        private async Task LoadHierarchyAsync(string teamId, string storeId)
        {
            var jdaPersonaClient = CreateClient(teamId);

            var siteId = int.Parse(storeId);
            // get the departments first because they are needed by the jobs
            await LoadDepartmentsAsync(jdaPersonaClient, teamId, siteId);
            await LoadJobsAsync(jdaPersonaClient, teamId, siteId);
        }

        private async Task LoadDepartmentsAsync(JdaPersonaClient jdaPersonaClient, string teamId, int siteId)
        {
            if (_teamDepartments.ContainsKey(teamId))
            {
                return;
            }

            var jdaDepartments = await jdaPersonaClient.ListDepartmentsAsync(siteId);

            var departments = jdaDepartments.ToDictionary(d => d.Id, d => d.Name);
            _teamDepartments[teamId] = new ConcurrentDictionary<int, string>(departments);
        }

        private async Task LoadJobsAsync(JdaPersonaClient jdaPersonaClient, string teamId, int siteId)
        {
            if (_teamJobs.ContainsKey(teamId))
            {
                return;
            }

            var jdaJobs = await jdaPersonaClient.ListJobsAsync(siteId);

            var departments = _teamDepartments[teamId];

            var jobs = jdaJobs.ToDictionary(job => job.Id.ToString(), job => new JobModel
            {
                SourceId = job.Id.ToString(),
                Name = job.Name,
                DepartmentName = job.DepartmentID.HasValue ? departments.GetValueOrDefault(job.DepartmentID.Value) : string.Empty,
                ThemeCode = job.DisplayCode
            });

            _teamJobs[teamId] = new ConcurrentDictionary<string, JobModel>(jobs);
        }

        public async Task<List<ShiftModel>> ListEmployeeWeekShiftsAsync(string teamId, string employeeId, DateTime weekStartDate, string TimeZoneInfoId)
        {
            var jdaPersonaClient = CreateClient(teamId);
            var empId = int.Parse(employeeId);
            var weekShifts = await jdaPersonaClient.GetEmployeeShiftsForWeekAsync(empId, weekStartDate);

            return MapShifts(weekShifts, TimeZoneInfoId);
        }

        private List<ShiftModel> MapShifts(WeekShifts weekShifts, string TimeZoneInfoId)
        {
            var shifts = new List<ShiftModel>();
            foreach (var weekShift in weekShifts.ScheduledShifts)
            {
                var shiftId = weekShift.ScheduledShiftId.ToString();
                var shift = new ShiftModel(shiftId)
                {
                    JdaEmployeeId = weekShift.EmployeeId,
                    JdaJobId = weekShift.ScheduledJobs?.First().JobId.ToString(),
                    LocalStartDate = weekShift.StartTime,
                    LocalEndDate = weekShift.EndTime,
                    StartDate = ConvertFromLocalTime(weekShift.StartTime, TimeZoneInfoId),
                    EndDate = ConvertFromLocalTime(weekShift.EndTime, TimeZoneInfoId),
                };

                shifts.Add(shift);

                if (weekShift.ScheduledJobs != null)
                {
                    foreach (var scheduledJob in weekShift.ScheduledJobs)
                    {
                        shift.Jobs.Add(new ActivityModel
                        {
                            JdaJobId = scheduledJob.JobId.ToString(),
                            LocalStartDate = scheduledJob.StartTime,
                            LocalEndDate = scheduledJob.EndTime,
                            StartDate = ConvertFromLocalTime(scheduledJob.StartTime, TimeZoneInfoId),
                            EndDate = ConvertFromLocalTime(scheduledJob.EndTime, TimeZoneInfoId)
                        });

                        if (scheduledJob.ScheduledDetail != null)
                        {
                            foreach (var scheduledDetail in scheduledJob.ScheduledDetail)
                            {
                                shift.Activities.Add(new ActivityModel
                                {
                                    Code = scheduledDetail.DetailTypeCode,
                                    LocalStartDate = scheduledDetail.StartTime,
                                    LocalEndDate = scheduledDetail.EndTime,
                                    StartDate = ConvertFromLocalTime(scheduledDetail.StartTime, TimeZoneInfoId),
                                    EndDate = ConvertFromLocalTime(scheduledDetail.EndTime, TimeZoneInfoId)
                                });
                            }
                        }
                    }
                }
            }

            return shifts;
        }

        public async Task<List<EmployeeModel>> GetEmployeesAsync(string teamId, string storeId, DateTime weekStartDate)
        {
            var jdaPersonaClient = CreateClient(teamId);
            var siteId = int.Parse(storeId);

            var weekEmployees = await jdaPersonaClient.GetSiteEmployeesAsync(siteId, weekStartDate);
            var employeeIds = weekEmployees.ClockEmployees.Select(e => e.EmployeeId.ToString()).ToList();

            await LoadEmployeesAsync(teamId, employeeIds);

            return _teamEmployees[teamId].Values.ToList();
        }
    }
}
