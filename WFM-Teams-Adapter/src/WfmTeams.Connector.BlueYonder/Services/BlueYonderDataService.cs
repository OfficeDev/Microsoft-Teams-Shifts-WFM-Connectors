// ---------------------------------------------------------------------------
// <copyright file="BlueYonderDataService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Rest;
    using WfmTeams.Adapter;
    using WfmTeams.Adapter.Exceptions;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Exceptions;
    using WfmTeams.Connector.BlueYonder.Extensions;
    using WfmTeams.Connector.BlueYonder.Helpers;
    using WfmTeams.Connector.BlueYonder.Mappings;
    using WfmTeams.Connector.BlueYonder.Models;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderDataService : BlueYonderBaseService, IWfmDataService
    {
        private readonly IAvailabilityMap _availabilityMap;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _teamDepartments = new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, JobModel>> _teamJobs = new ConcurrentDictionary<string, ConcurrentDictionary<string, JobModel>>();
        private readonly ISystemTimeService _timeService;
        private readonly ICacheService _cacheService;
        private readonly ITimeZoneService _timeZoneService;

        public BlueYonderDataService(BlueYonderPersonaOptions options, IBlueYonderClientFactory clientFactory, ISecretsService secretsService, IAvailabilityMap availabilityMap, ISystemTimeService timeService, IStringLocalizer<BlueYonderConfigService> stringLocalizer, ICacheService cacheService, ITimeZoneService timeZoneService)
            : base(options, secretsService, clientFactory, stringLocalizer)
        {
            _availabilityMap = availabilityMap ?? throw new ArgumentNullException(nameof(availabilityMap));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        }

        public async Task<List<string>> GetDepartmentsAsync(string teamId, string storeId)
        {
            if (!_teamDepartments.ContainsKey(teamId))
            {
                await LoadHierarchyAsync(teamId, storeId);
            }
            return _teamDepartments[teamId].Values.ToList();
        }

        public async Task<List<string>> GetEligibleTargetsForShiftSwap(ShiftModel shift, EmployeeModel employee, string buId)
        {
            var blueYonderClient = CreateEssPublicClient(employee);

            try
            {
                var swapResponse = await blueYonderClient.GetAvailableSwapShiftsAsync(shift.WfmShiftId).ConfigureAwait(false);
                if (swapResponse.Entities != null)
                {
                    return swapResponse.Entities.Select(s => s.ShiftId.ToString()).ToList();
                }

                return new List<string>();
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                var wfmError = new WfmError();
                wfmError.Code = BYErrorCodes.UserUnauthorized;
                wfmError.Message = _stringLocalizer[BYErrorCodes.UserUnauthorized];
                throw new WfmException(wfmError, ex);
            }
        }

        public async Task<List<EmployeeModel>> GetEmployeesAsync(string teamId, string storeId, DateTime weekStartDate)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);
            var siteId = int.Parse(storeId);

            var weekEmployees = await blueYonderClient.GetSiteEmployeesAsync(siteId, weekStartDate).ConfigureAwait(false);
            var employeeIds = weekEmployees.Entities.Select(e => e.EmployeeId.ToString()).ToList();

            // get all the users from the Blue Yonder api in batches of maximum users
            List<EmployeeModel> employees = new List<EmployeeModel>();
            foreach (var batch in employeeIds.Buffer(_options.MaximumUsers))
            {
                var userCollection = await blueYonderClient.GetUsersAsync(batch).ConfigureAwait(false);

                employees.AddRange(userCollection.Entities
                    .Select(byUser => new EmployeeModel
                    {
                        WfmEmployeeId = byUser.UserId.ToString(),
                        WfmLoginName = byUser.Name?.LoginName,
                        DisplayName = $"{byUser.Name?.FirstName} {byUser.Name?.LastName}",
                        IsManager = byUser.UserSecurityGroupAssignmentCollection.Entities.Count(s => s.SecurityGroupId == _options.StoreManagerSecurityGroupId) > 0
                    })
                    .ToList());

                // wait for the configured interval before processing the next batch
                await Task.Delay(_options.BatchDelayMs).ConfigureAwait(false);
            }

            return employees;
        }

        public async Task<JobModel> GetJobAsync(string teamId, string storeId, string jobId)
        {
            if (!_teamJobs.ContainsKey(teamId))
            {
                await LoadHierarchyAsync(teamId, storeId).ConfigureAwait(false);
            }

            if (!_teamJobs[teamId].ContainsKey(jobId))
            {
                // the job is presumably not associated with the store, so fetch it separately and
                // add it to the list of team jobs
                var job = await LoadJobAsync(teamId, jobId).ConfigureAwait(false);
                if (job != null)
                {
                    _teamJobs[teamId][jobId] = job;
                }
            }

            return _teamJobs[teamId].ReplGetValueOrDefault(jobId);
        }

        public async Task<BusinessUnitModel> GetBusinessUnitAsync(string buId, ILogger log)
        {
            var siteId = Guard.ArgumentIsInteger(buId, nameof(buId));
            var client = await CreatePublicClientAsync().ConfigureAwait(false);

            try
            {
                var site = await client.GetSiteByIdAsync(siteId).ConfigureAwait(false);

                var timeZoneInfoId = _options.DefaultWfmTimeZone;
                if (site.TimeZoneAssignmentID.HasValue)
                {
                    var wfmTimeZoneName = await GetTimeZoneNameAsync(site.TimeZoneAssignmentID.Value, client).ConfigureAwait(false);
                    try
                    {
                        timeZoneInfoId = await _timeZoneService.GetTimeZoneInfoIdAsync(wfmTimeZoneName).ConfigureAwait(false);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        // the BY time zone name was not found in the map table
                        log.LogTimeZoneError(ex, site.Id, site.TimeZoneAssignmentID.Value, wfmTimeZoneName);
                    }
                }

                return new BusinessUnitModel
                {
                    WfmBuId = site.Id.ToString(),
                    WfmBuName = site.Name,
                    TimeZoneInfoId = timeZoneInfoId
                };
            }
            catch (HttpOperationException hex) when (hex.Response?.ReasonPhrase == "Not Found")
            {
                throw new KeyNotFoundException($"Business unit with ID {buId} was not found.");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Failed to get the business unit from Blue Yonder", ex);
            }
        }

        public async Task<List<EmployeeAvailabilityModel>> ListEmployeeAvailabilityAsync(string teamId, List<string> employeeIds, string timeZoneInfoId)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);

            // get all the availability for all employees in the store from Blue Yonder api in
            // batches of maximum users
            var employeeAvailabilityCollections = new List<EmployeeAvailabilityCollectionResource>();
            foreach (var batch in employeeIds.Buffer(_options.MaximumUsers))
            {
                var tasks = batch.Select(empId => blueYonderClient.GetEmployeeAvailabilityAsync(int.Parse(empId)));
                var result = await Task.WhenAll(tasks).ConfigureAwait(false);

                employeeAvailabilityCollections.AddRange(result);

                // wait for the configured interval before processing the next batch
                await Task.Delay(_options.BatchDelayMs).ConfigureAwait(false);
            }

            var employeeAvailabilityList = new List<EmployeeAvailabilityModel>();
            foreach (var empAvailabilityCollection in employeeAvailabilityCollections)
            {
                var model = _availabilityMap.MapAvailability(empAvailabilityCollection, timeZoneInfoId);
                if (!string.IsNullOrEmpty(model.WfmId))
                {
                    employeeAvailabilityList.Add(model);
                }
            }

            return employeeAvailabilityList;
        }

        public async Task<List<ShiftModel>> ListWeekShiftsAsync(string teamId, string storeId, DateTime weekStartDate, string timeZoneInfoId)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);
            var siteId = int.Parse(storeId);
            var weekShifts = await blueYonderClient.GetSiteShiftsForWeekAsync(siteId, weekStartDate.AsDateString()).ConfigureAwait(false);

            return MapShifts(weekShifts, timeZoneInfoId);
        }

        public async Task<List<ShiftModel>> ListWeekOpenShiftsAsync(string teamId, string storeId, DateTime weekStartDate, string timeZoneInfoId, EmployeeModel storeManager)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);

            var siteId = int.Parse(storeId);

            var response = await blueYonderClient.GetUnfilledShiftsAsync(siteId, weekStartDate.AsDateString()).ConfigureAwait(false);

            var tasks = response.Entities.Select(s => MapOpenShiftAsync(s, teamId, storeId, timeZoneInfoId)).ToList();
            var allTasks = Task.WhenAll(tasks);
            var result = await allTasks;

            return result.ToList();
        }

        public async Task<List<TimeOffModel>> ListWeekTimeOffAsync(string teamId, List<string> employeeIds, DateTime weekStartDate, string timeZoneInfoId)
        {
            var weekEndDate = weekStartDate.AddWeek().AddSeconds(-1);

            // get the cached time off records
            var timeOffRecords = await _cacheService.GetKeyAsync<List<TimeOffModel>>(BYConstants.TableNameTimeOffData, teamId);

            if (timeOffRecords != null)
            {
                return timeOffRecords.Where(t => t.StartDate >= weekStartDate && t.StartDate <= weekEndDate).ToList();
            }
            else
            {
                return new List<TimeOffModel>();
            }
        }

        public async Task<bool> PrepareSyncAsync(PrepareSyncModel syncModel, ILogger log)
        {
            var result = true;

            switch (syncModel.Type)
            {
                case SyncType.TimeOff:
                    result = await CacheTimeOffDataAsync(syncModel, log);
                    break;

                default:
                    // nothing to do
                    break;
            }

            return result;
        }

        private async Task<string> GetTimeZoneNameAsync(int timeZoneId, IBlueYonderClient client)
        {
            var timeZone = await client.GetTimeZoneByIdAsync(timeZoneId);
            return timeZone.Name;
        }

        private async Task<List<TimeOffReasonModel>> ListTimeOffReasonsAsync(List<string> timeOffReasonIds)
        {
            var retailWebClient = await CreatePublicClientAsync().ConfigureAwait(false);

            // get all the specified time off reasons in the store from Blue Yonder api
            var tasks = timeOffReasonIds.Select(torId => retailWebClient.GetTimeOffTypeAsync(int.Parse(torId)));
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);

            return result.Select(timeOffReason => new TimeOffReasonModel
            {
                WfmTimeOffReasonId = timeOffReason.Id.ToString(),
                Reason = timeOffReason.Name
            }).ToList();
        }

        private async Task LoadDepartmentsAsync(IBlueYonderClient blueYonderClient, string teamId, int siteId)
        {
            if (_teamDepartments.ContainsKey(teamId))
            {
                return;
            }

            var byDepartments = await blueYonderClient.ListDepartmentsAsync(siteId).ConfigureAwait(false);

            var departments = byDepartments.Entities.ToDictionary(d => d.Id, d => d.Name);
            _teamDepartments[teamId] = new ConcurrentDictionary<int, string>(departments);
        }

        private async Task LoadHierarchyAsync(string teamId, string storeId)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);

            var siteId = int.Parse(storeId);
            // get the departments first because they are needed by the jobs
            await LoadDepartmentsAsync(blueYonderClient, teamId, siteId).ConfigureAwait(false);
            await LoadJobsAsync(blueYonderClient, teamId, siteId).ConfigureAwait(false);
        }

        private async Task<JobModel> LoadJobAsync(string teamId, string jobId)
        {
            var blueYonderClient = await CreatePublicClientAsync().ConfigureAwait(false);

            var departments = _teamDepartments[teamId];
            var id = int.Parse(jobId);
            var job = await blueYonderClient.GetJobByIdAsync(id).ConfigureAwait(false);

            return MapJobToJobModel(job, departments);
        }

        private async Task LoadJobsAsync(IBlueYonderClient blueYonderClient, string teamId, int siteId)
        {
            if (_teamJobs.ContainsKey(teamId))
            {
                return;
            }

            var byJobs = await blueYonderClient.ListJobsAsync(siteId).ConfigureAwait(false);

            var departments = _teamDepartments[teamId];

            var jobs = byJobs.Entities.ToDictionary(job => job.JobId.ToString(), job => MapJobToJobModel(job, departments));

            _teamJobs[teamId] = new ConcurrentDictionary<string, JobModel>(jobs);
        }

        private JobModel MapJobToJobModel(Job job, ConcurrentDictionary<int, string> departments)
        {
            return new JobModel
            {
                WfmId = job.JobId.ToString(),
                Name = job.Name,
                DepartmentName = job.DepartmentID.HasValue ? departments.ReplGetValueOrDefault(job.DepartmentID.Value) : string.Empty,
                ThemeCode = job.DisplayCode
            };
        }

        private List<ShiftModel> MapShifts(Models.WeekShifts weekShifts, string timeZoneInfoId)
        {
            var shifts = new List<ShiftModel>();
            foreach (var weekShift in weekShifts.Entities)
            {
                var shiftId = weekShift.ScheduledShiftId.ToString();
                var shift = new ShiftModel(shiftId)
                {
                    WfmEmployeeId = weekShift.EmployeeId.ToString(),
                    WfmJobId = weekShift.ScheduledJobs?.First().JobId.ToString(),
                    StartDate = weekShift.StartTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                    EndDate = weekShift.EndTime.ConvertFromLocalTime(timeZoneInfoId, _timeService)
                };

                shifts.Add(shift);

                if (weekShift.ScheduledJobs != null)
                {
                    foreach (var scheduledJob in weekShift.ScheduledJobs)
                    {
                        shift.Jobs.Add(new ActivityModel
                        {
                            WfmJobId = scheduledJob.JobId.ToString(),
                            StartDate = scheduledJob.StartTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                            EndDate = scheduledJob.EndTime.ConvertFromLocalTime(timeZoneInfoId, _timeService)
                        });

                        if (scheduledJob.ScheduledDetail != null)
                        {
                            foreach (var scheduledDetail in scheduledJob.ScheduledDetail)
                            {
                                shift.Activities.Add(new ActivityModel
                                {
                                    Code = scheduledDetail.DetailTypeCode,
                                    StartDate = scheduledDetail.StartTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                                    EndDate = scheduledDetail.EndTime.ConvertFromLocalTime(timeZoneInfoId, _timeService)
                                });
                            }
                        }
                    }
                }
            }

            return shifts;
        }

        private async Task<ShiftModel> MapOpenShiftAsync(OpenShift openShift, string teamId, string storeId, string timeZoneInfoId)
        {
            var firstJob = openShift.ScheduledJobs?.First();

            var jobResult = await GetJobAsync(teamId, storeId, firstJob.JobId.ToString()).ConfigureAwait(false);

            var openShiftModel = new ShiftModel(openShift.ScheduledShiftId.ToString())
            {
                WfmJobId = firstJob.JobId.ToString(),
                WfmJobName = jobResult.Name,
                ThemeCode = jobResult.ThemeCode,
                StartDate = openShift.StartTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                EndDate = openShift.EndTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                Quantity = openShift.Quantity ?? 1
            };

            if (openShift.ScheduledJobs != null)
            {
                foreach (var job in openShift.ScheduledJobs)
                {
                    var scheduleJob = await GetJobAsync(teamId, storeId, job.JobId.ToString()).ConfigureAwait(false);

                    openShiftModel.Jobs.Add(new ActivityModel
                    {
                        WfmJobId = job.JobId.ToString(),
                        Code = scheduleJob.Name,
                        ThemeCode = scheduleJob.ThemeCode,
                        StartDate = job.StartTime.ConvertFromLocalTime(timeZoneInfoId, _timeService),
                        EndDate = job.EndTime.ConvertFromLocalTime(timeZoneInfoId, _timeService)
                    });
                }
            }

            return openShiftModel;
        }

        private async Task<bool> CacheTimeOffDataAsync(PrepareSyncModel syncModel, ILogger log)
        {
            var client = await CreatePublicClientAsync().ConfigureAwait(false);
            var endDate = syncModel.LastWeekStartDate.AddWeek().AddSeconds(-1);

            if (_options.TimeOffReasonWhitelist == null)
            {
                // as no time off reasons have been whitelisted, no time off records can be processed
                return false;
            }

            // get all the employees for the store
            var employeeIds = await CacheHelper.GetEmployeeIdListAsync(_cacheService, syncModel.TeamId);

            // get all the time off for all employees in the store from Blue Yonder api in batches
            // of maximum users
            List<TimeOffModel> timeOffRecords = new List<TimeOffModel>();
            foreach (var batch in employeeIds.Buffer(_options.MaximumUsers))
            {
                var tasks = batch.Select(empId => client.GetEmployeeTimeOffByYearAsync(int.Parse(empId), syncModel.FirstWeekStartDate.Year)).ToList();
                if (endDate.Year != syncModel.FirstWeekStartDate.Year)
                {
                    // also get the following year's data
                    tasks.AddRange(batch.Select(empId => client.GetEmployeeTimeOffByYearAsync(int.Parse(empId), endDate.Year)));
                }
                var allTasks = Task.WhenAll(tasks);

                var result = await allTasks.ConfigureAwait(false);
                if (allTasks.Status == TaskStatus.Faulted)
                {
                    // there were one or more errors getting the time off records, so throw the
                    // aggregate exception to ensure that we do not continue to sync time off
                    // records with an incomplete set of records
                    throw allTasks.Exception;
                }

                // filter the time off records to those that have been approved and that start some
                // time within the specified period
                timeOffRecords.AddRange(result.SelectMany(tors => tors.Entities)
                    .Where(t => t.Status.Equals("approved", StringComparison.OrdinalIgnoreCase) && t.Start >= syncModel.FirstWeekStartDate && t.Start <= endDate)
                    .Select(timeOff => new TimeOffModel
                    {
                        WfmTimeOffId = timeOff.TimeOffRequestId.ToString(),
                        WfmTimeOffTypeId = timeOff.TimeOffTypeId.ToString(),
                        WfmEmployeeId = timeOff.EmployeeId.ToString(),
                        StartDate = timeOff.Start.ConvertFromLocalTime(syncModel.TimeZoneInfoId, _timeService),
                        EndDate = timeOff.End.ConvertFromLocalTime(syncModel.TimeZoneInfoId, _timeService)
                    }).ToList());

                // wait for the configured interval before processing the next batch
                await Task.Delay(_options.BatchDelayMs).ConfigureAwait(false);
            }

            // get the list of time off reasons
            var timeOffReasonIds = timeOffRecords.Distinct(t => t.WfmTimeOffTypeId).Select(t => t.WfmTimeOffTypeId).ToList();
            var timeOffReasonRecords = await ListTimeOffReasonsAsync(timeOffReasonIds).ConfigureAwait(false);
            var allowedTimeOffReasons = _options.TimeOffReasonWhitelist.Split(',', ';');
            var allowedTimeOffReasonIds = timeOffReasonRecords.Where(tor => allowedTimeOffReasons.Contains(tor.Reason, StringComparer.InvariantCultureIgnoreCase)).Select(tor => tor.WfmTimeOffReasonId);

            // cache the list of time off records filtered to only those records with time off
            // reasons in the whitelist
            var allowedTimeOffRecords = timeOffRecords.Where(t => allowedTimeOffReasonIds.Contains(t.WfmTimeOffTypeId)).ToList();

            foreach (var allowedTimeOffRecord in allowedTimeOffRecords)
            {
                allowedTimeOffRecord.WfmTimeOffReason = timeOffReasonRecords.First(tor => tor.WfmTimeOffReasonId == allowedTimeOffRecord.WfmTimeOffTypeId).Reason;
            }

            await _cacheService.SetKeyAsync(BYConstants.TableNameTimeOffData, syncModel.TeamId, allowedTimeOffRecords);

            return true;
        }
    }
}