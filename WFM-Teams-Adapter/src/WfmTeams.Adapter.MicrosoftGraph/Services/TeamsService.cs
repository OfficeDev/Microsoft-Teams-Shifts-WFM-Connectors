// ---------------------------------------------------------------------------
// <copyright file="TeamsService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Rest;
    using Polly;
    using Polly.Retry;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.MicrosoftGraph.Extensions;
    using WfmTeams.Adapter.MicrosoftGraph.Mappings;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class TeamsService : ITeamsService
    {
        private readonly IAvailabilityMap _availabilityMap;
        private readonly ICacheService _cacheService;
        private readonly IMicrosoftGraphClientFactory _clientFactory;
        private readonly MicrosoftGraphOptions _options;
        private readonly IShiftMap _shiftMap;
        private readonly ITimeOffMap _timeOffMap;

        public TeamsService(MicrosoftGraphOptions options, IMicrosoftGraphClientFactory clientFactory, IShiftMap shiftMap, ICacheService cacheService, ITimeOffMap timeOffMap, IAvailabilityMap availabilityMap)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _shiftMap = shiftMap ?? throw new ArgumentNullException(nameof(shiftMap));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _timeOffMap = timeOffMap ?? throw new ArgumentNullException(nameof(timeOffMap));
            _availabilityMap = availabilityMap ?? throw new ArgumentNullException(nameof(availabilityMap));
        }

        public async Task AddUsersToSchedulingGroupAsync(string teamId, string teamsSchedulingGroupId, List<string> userIds)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var policy = GetConflictRetryPolicy(_options.RetryMaxAttempts, _options.RetryIntervalSeconds);
            await policy.ExecuteAsync(() => ReplaceSchedulingGroupAsync(client, teamId, teamsSchedulingGroupId, userIds)).ConfigureAwait(false);
        }

        public async Task ApproveOpenShiftRequest(string requestId, string teamId)
        {
            var client = _clientFactory.CreateClientNoPassthrough(_options, teamId);

            var messageItem = new MessageItem();
            await client.ReviewOpenShiftRequestAsync(messageItem, teamId, requestId, "approve").ConfigureAwait(false);
        }

        public async Task ApproveSwapShiftsRequest(string message, string requestId, string teamId)
        {
            var client = _clientFactory.CreateClientNoPassthrough(_options, teamId);

            var swapShiftsChangeRequest = new SwapShiftsChangeRequest
            {
                Message = message
            };

            await client.ApproveSwapShiftRequestAsync(swapShiftsChangeRequest, teamId, requestId).ConfigureAwait(false);
        }

        public async Task CreateOpenShiftAsync(string teamId, ShiftModel openShift)
        {
            Guard.ArgumentNotNullOrEmpty(openShift.TeamsSchedulingGroupId, nameof(ShiftModel.TeamsSchedulingGroupId));

            var client = _clientFactory.CreateClient(_options, teamId);

            // map the shift to a Teams ShiftItem
            var openShiftItem = _shiftMap.MapOpenShift(openShift);

            var request = new OpenShiftRequest
            {
                SchedulingGroupId = openShift.TeamsSchedulingGroupId,
                DraftOpenShift = _options.DraftShiftsEnabled ? openShiftItem : null,
                SharedOpenShift = _options.DraftShiftsEnabled ? null : openShiftItem
            };

            var response = await client.CreateOpenShiftAsync(request, teamId).ConfigureAwait(false);
            response.ThrowIfError();

            // update the shift with the Teams id for the shift
            openShift.TeamsShiftId = ((OpenShiftResponse)response).Id;
        }

        public async Task CreateScheduleAsync(string teamId, ScheduleModel schedule)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var scheduleRequest = new ScheduleRequest(schedule);

            await client.CreateReplaceScheduleAsync(scheduleRequest, teamId).ConfigureAwait(false);
        }

        public async Task<string> CreateSchedulingGroupAsync(string teamId, string groupName, List<string> userIds)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // ensure that all users are actually in the Team and remove any that aren't
            var teamMembers = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamId).ConfigureAwait(false);
            RemoveInvalidUsers(userIds, teamMembers);

            // because we are caching the scheduling groups we cannot be certain that a group has
            // not been created by another instance of the service, so try and get the group from
            // the refreshed cache, and only if it is still not found, create it
            var groupId = await GetSchedulingGroupIdByNameAsync(client, teamId, groupName).ConfigureAwait(false);
            if (string.IsNullOrEmpty(groupId))
            {
                var request = new SchedulingGroupRequest
                {
                    DisplayName = groupName,
                    IsActive = true
                };

                if (userIds.Count > 0)
                {
                    request.UserIds = userIds;
                }

                var response = await client.CreateSchedulingGroupAsync(request, teamId).ConfigureAwait(false);
                response.ThrowIfError();

                groupId = ((SchedulingGroupResponse)response).Id;

                // update the cache of scheduling groups
                await RefreshCachedSchedulingGroupsAsync(client, teamId).ConfigureAwait(false);
            }

            return groupId;
        }

        public async Task CreateShiftAsync(string teamId, ShiftModel shift)
        {
            Guard.ArgumentNotNullOrEmpty(shift.TeamsSchedulingGroupId, nameof(ShiftModel.TeamsSchedulingGroupId));
            Guard.ArgumentNotNullOrEmpty(shift.TeamsEmployeeId, nameof(ShiftModel.TeamsEmployeeId));

            var client = _clientFactory.CreateClient(_options, teamId);

            var request = new ShiftRequest
            {
                SchedulingGroupId = shift.TeamsSchedulingGroupId,
                UserId = shift.TeamsEmployeeId,
                DraftShift = _options.DraftShiftsEnabled ? _shiftMap.MapShift(shift) : null,
                SharedShift = _options.DraftShiftsEnabled ? null : _shiftMap.MapShift(shift)
            };

            var response = await client.CreateShiftAsync(request, teamId).ConfigureAwait(false);
            response.ThrowIfError();

            // update the shift with the Teams id for the shift
            shift.TeamsShiftId = ((ShiftResponse)response).Id;
        }

        public async Task CreateTimeOffAsync(string teamId, TimeOffModel timeOff)
        {
            Guard.ArgumentNotNullOrEmpty(timeOff.TeamsEmployeeId, nameof(TimeOffModel.TeamsEmployeeId));

            var client = _clientFactory.CreateClient(_options, teamId);

            var request = new TimeOffRequest
            {
                UserId = timeOff.TeamsEmployeeId,
                DraftTimeOff = _options.DraftShiftsEnabled ? _timeOffMap.MapTimeOff(timeOff) : null,
                SharedTimeOff = _options.DraftShiftsEnabled ? null : _timeOffMap.MapTimeOff(timeOff)
            };

            var response = await client.CreateTimeOffAsync(request, teamId).ConfigureAwait(false);
            response.ThrowIfError();

            // update the time off with the Teams id for the time off
            timeOff.TeamsTimeOffId = ((TimeOffResponse)response).Id;
        }

        public async Task<TimeOffReasonModel> CreateTimeOffReasonAsync(string teamId, TimeOffReasonModel timeOffType)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            var request = new TimeOffReasonRequest
            {
                DisplayName = timeOffType.Reason,
                IsActive = true
            };

            var response = await client.CreateTimeOffReasonAsync(request, teamId).ConfigureAwait(false);
            timeOffType.TeamsTimeOffReasonId = response.Id;

            return timeOffType;
        }

        public async Task DeclineOpenShiftRequest(string requestId, string teamId, string message)
        {
            var client = _clientFactory.CreateClientNoPassthrough(_options, teamId);

            var messageItem = new MessageItem
            {
                Message = message
            };
            await client.ReviewOpenShiftRequestAsync(messageItem, teamId, requestId, "decline").ConfigureAwait(false);
        }

        public async Task DeleteAvailabilityAsync(EmployeeAvailabilityModel availability)
        {
            Guard.ArgumentNotNullOrEmpty(availability.TeamsEmployeeId, nameof(EmployeeAvailabilityModel.TeamsEmployeeId));

            var appClient = _clientFactory.CreateUserClient(_options, availability.TeamsEmployeeId);

            // Teams does not support deleting of availability, so all we can do is to restore
            // availability to the default of all-day for all days of the week, however, we should
            // check to see if it is already default because if so, we should skip the operation

            // get the existing shift preference from Teams
            var shiftPreference = await appClient.GetUserShiftPreferenceAsync(availability.TeamsEmployeeId).ConfigureAwait(false);

            var defaultWeekAvailability = CreateDefaultAvailabilityForWeek(availability.TimeZoneInfoId);
            if (!AvailabilityMatches(shiftPreference.Availability, defaultWeekAvailability))
            {
                shiftPreference.Availability = defaultWeekAvailability;
                await appClient.UpdateUserShiftPreferenceAsync(shiftPreference, availability.TeamsEmployeeId).ConfigureAwait(false);
            }
        }

        public async Task DeleteOpenShiftAsync(string teamId, ShiftModel openShift, bool draftDelete)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            if (_options.DraftOpenShiftDeletesEnabled || draftDelete)
            {
                await client.StageDeleteOpenShiftAsync(teamId, openShift.TeamsShiftId).ConfigureAwait(false);
            }
            else
            {
                await client.DeleteOpenShiftAsync(teamId, openShift.TeamsShiftId).ConfigureAwait(false);
            }
        }

        public async Task DeleteShiftAsync(string teamId, ShiftModel shift, bool draftDelete)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            if (_options.DraftShiftDeletesEnabled || draftDelete)
            {
                await client.StageDeleteShiftAsync(teamId, shift.TeamsShiftId).ConfigureAwait(false);
            }
            else
            {
                await client.DeleteShiftAsync(teamId, shift.TeamsShiftId).ConfigureAwait(false);
            }
        }

        public async Task DeleteTimeOffAsync(string teamId, TimeOffModel timeOff, bool draftDelete)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            if (_options.DraftTimeOffDeletesEnabled || draftDelete)
            {
                await client.StageDeleteTimeOffAsync(teamId, timeOff.TeamsTimeOffId).ConfigureAwait(false);
            }
            else
            {
                await client.DeleteTimeOffAsync(teamId, timeOff.TeamsTimeOffId).ConfigureAwait(false);
            }
        }

        public async Task<EmployeeAvailabilityModel> GetEmployeeAvailabilityAsync(string userId)
        {
            var userClient = _clientFactory.CreateUserClient(_options, userId);

            var response = await userClient.GetUserShiftPreferenceAsync(userId).ConfigureAwait(false);

            return _availabilityMap.MapAvailability(response.Availability, userId);
        }

        public async Task<List<EmployeeModel>> GetEmployeesAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // get the list of members of the team from teams - 999 is the maximum that can be
            // returned in a single call
            var response = await client.GetMembersAsync(teamId, "id,displayName,userPrincipalName", 999).ConfigureAwait(false);
            return response.Value
                .Select(m => new EmployeeModel
                {
                    TeamsEmployeeId = m.Id,
                    DisplayName = m.DisplayName,
                    TeamsLoginName = m.UserPrincipalName
                }).ToList();
        }

        public async Task<ScheduleModel> GetScheduleAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var scheduleResponse = await client.GetScheduleAsync(teamId).ConfigureAwait(false);
            return new ScheduleModel
            {
                IsEnabled = scheduleResponse.Enabled,
                Status = scheduleResponse.ProvisionStatus,
                TimeZone = scheduleResponse.TimeZone,
                TimeClockEnabled = scheduleResponse.TimeClockEnabled.HasValue ? scheduleResponse.TimeClockEnabled.Value : false,
                OpenShiftsEnabled = scheduleResponse.OpenShiftsEnabled.HasValue ? scheduleResponse.OpenShiftsEnabled.Value : false,
                SwapShiftsRequestsEnabled = scheduleResponse.SwapShiftsRequestsEnabled.HasValue ? scheduleResponse.SwapShiftsRequestsEnabled.Value : false,
                OfferShiftRequestsEnabled = scheduleResponse.OfferShiftRequestsEnabled.HasValue ? scheduleResponse.OfferShiftRequestsEnabled.Value : false,
                TimeOffRequestsEnabled = scheduleResponse.TimeOffRequestsEnabled.HasValue ? scheduleResponse.TimeOffRequestsEnabled.Value : false,
            };
        }

        public async Task<string> GetSchedulingGroupIdByNameAsync(string teamId, string groupName)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var schedulingGroups = await GetSchedulingGroupsAsync(client, teamId).ConfigureAwait(false);

            var group = schedulingGroups.Find(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            return group != null ? group.Id : null;
        }

        public async Task<List<SchedulingGroupModel>> GetSchedulingGroupsAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            return await GetSchedulingGroupsAsync(client, teamId);
        }

        public async Task<GroupModel> GetTeamAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            var response = await client.GetTeamAsync(teamId, "id,displayName,createdDateTime,deletedDateTime").ConfigureAwait(false);
            response.ThrowIfError();

            var team = (TeamResponse)response;
            return new GroupModel
            {
                Id = team.Id,
                Name = team.DisplayName,
                CreatedDateTime = team.CreatedDateTime,
                DeletedDateTime = team.DeletedDateTime
            };
        }

        public async Task<List<string>> ListActiveSchedulingGroupIdsAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            var response = await client.ListSchedulingGroupsAsync(teamId).ConfigureAwait(false);

            return response.Value
                .Where(i => i.IsActive)
                .Select(i => i.Id)
                .ToList();
        }

        public async Task<List<ShiftModel>> ListOpenShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // get initial set of open shifts for the given date range
            var filter = $"sharedOpenShift/startDateTime ge {startDate:o} and sharedOpenShift/endDateTime le {endDate:o}";
            var response = await client.ListOpenShiftsAsync(filter, teamId, batchSize).ConfigureAwait(false);
            var mappedShifts = MapOpenShiftsToShiftModel(new List<ShiftModel>(), response);
            var nextLink = response.ODataNextLink;

            // Check for more shifts that require deleting and retrieve the additional pages if needed
            while (!string.IsNullOrEmpty(nextLink))
            {
                response = await client.ListOpenShiftsNextPageAsync(nextLink);
                mappedShifts = MapOpenShiftsToShiftModel(mappedShifts, response);
                nextLink = response.ODataNextLink;
            }

            return mappedShifts;
        }

        public async Task<List<ShiftModel>> ListShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // get initial set of shifts for the given date range
            var filter = $"sharedShift/startDateTime ge {startDate:o} and sharedShift/endDateTime le {endDate:o}";
            var response = await client.ListShiftsAsync(filter, teamId, batchSize).ConfigureAwait(false);
            var mappedShifts = MapToShiftModel(new List<ShiftModel>(), response);
            var nextLink = response.ODataNextLink;

            // Check for more shifts that require deleting and retrieve the additional pages if needed
            while (!string.IsNullOrEmpty(nextLink))
            {
                response = await client.ListShiftsNextPageAsync(nextLink);
                mappedShifts = MapToShiftModel(mappedShifts, response);
                nextLink = response.ODataNextLink;
            }

            return mappedShifts;
        }

        public async Task<List<TimeOffModel>> ListTimeOffAsync(string teamId, DateTime startDate, DateTime endDate, int batchSize)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // get initial set of time off for the given date range
            var filter = $"sharedTimeOff/startDateTime ge {startDate:o} and sharedTimeOff/endDateTime le {endDate:o}";
            var response = await client.ListTimeOffAsync(filter, teamId, batchSize).ConfigureAwait(false);
            var mappedTimeOff = MapTimeOffToTimeOffModel(new List<TimeOffModel>(), response);
            var nextLink = response.ODataNextLink;

            // Check for more timeOff that requires deleting and retrieve the additional pages if needed
            while (!string.IsNullOrEmpty(nextLink))
            {
                response = await client.ListTimeOffNextPageAsync(nextLink);
                mappedTimeOff = MapTimeOffToTimeOffModel(mappedTimeOff, response);
                nextLink = response.ODataNextLink;
            }

            return mappedTimeOff;
        }

        public async Task<List<TimeOffReasonModel>> ListTimeOffReasonsAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            var response = await client.ListTimeOffReasonsAsync(teamId).ConfigureAwait(false);

            return response.Value.Where(r => r.IsActive).Select(r => new TimeOffReasonModel
            {
                TeamsTimeOffReasonId = r.Id,
                Reason = r.DisplayName
            }).ToList();
        }

        public Task RemoveUsersFromSchedulingGroupAsync(string teamId, string schedulingGroupId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var policy = GetConflictRetryPolicy(_options.RetryMaxAttempts, _options.RetryIntervalSeconds);

            return policy.ExecuteAsync(async () =>
            {
                await RemoveUsersFromSchedulingGroupAsync(client, teamId, schedulingGroupId).ConfigureAwait(false);
            });
        }

        public async Task ShareScheduleAsync(string teamId, DateTime startTime, DateTime endTime, bool notifyTeam)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var shareRequest = new ShareRequest
            {
                StartDateTime = startTime,
                EndDateTime = endTime,
                NotifyTeam = notifyTeam
            };
            var policy = GetTimeoutRetryPolicy(_options.LongOperationMaxAttempts, _options.LongOperationRetryIntervalSeconds);
            await policy.ExecuteAsync(() => client.ShareScheduleAsync(shareRequest, teamId)).ConfigureAwait(false);
        }

        public async Task UpdateAvailabilityAsync(EmployeeAvailabilityModel availability)
        {
            Guard.ArgumentNotNullOrEmpty(availability.TeamsEmployeeId, nameof(EmployeeAvailabilityModel.TeamsEmployeeId));

            var appClient = _clientFactory.CreateUserClient(_options, availability.TeamsEmployeeId);

            // get the existing shift preference from Teams
            var shiftPreference = await appClient.GetUserShiftPreferenceAsync(availability.TeamsEmployeeId).ConfigureAwait(false);

            shiftPreference.Availability = _availabilityMap.MapAvailability(availability);
            await appClient.UpdateUserShiftPreferenceAsync(shiftPreference, availability.TeamsEmployeeId).ConfigureAwait(false);
        }

        public async Task UpdateOpenShiftAsync(string teamId, ShiftModel openShift)
        {
            Guard.ArgumentNotNullOrEmpty(openShift.TeamsShiftId, nameof(ShiftModel.TeamsShiftId));
            Guard.ArgumentNotNullOrEmpty(openShift.TeamsSchedulingGroupId, nameof(ShiftModel.TeamsSchedulingGroupId));

            var client = _clientFactory.CreateClient(_options, teamId);

            // get the existing open shift from Teams
            var openShiftResponse = await client.GetOpenShiftAsync(teamId, openShift.TeamsShiftId).ConfigureAwait(false);
            openShiftResponse.SchedulingGroupId = openShift.TeamsSchedulingGroupId;
            if (_options.DraftShiftsEnabled)
            {
                openShiftResponse.DraftOpenShift = _shiftMap.MapOpenShift(openShift);
            }
            else
            {
                openShiftResponse.SharedOpenShift = _shiftMap.MapOpenShift(openShift);
            }

            await client.ReplaceOpenShiftAsync(openShiftResponse, string.Empty, teamId, openShiftResponse.Id).ConfigureAwait(false);
        }

        public async Task UpdateScheduleAsync(string teamId, ScheduleModel schedule)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var scheduleResponse = await client.GetScheduleAsync(teamId).ConfigureAwait(false);
            // update the schedule with the changes
            var scheduleRequest = new ScheduleRequest(schedule)
            {
                Id = scheduleResponse.Id
            };

            await client.CreateReplaceScheduleAsync(scheduleRequest, teamId).ConfigureAwait(false);
        }

        public async Task UpdateShiftAsync(string teamId, ShiftModel shift)
        {
            Guard.ArgumentNotNullOrEmpty(shift.TeamsSchedulingGroupId, nameof(ShiftModel.TeamsSchedulingGroupId));
            Guard.ArgumentNotNullOrEmpty(shift.TeamsEmployeeId, nameof(ShiftModel.TeamsEmployeeId));

            var client = _clientFactory.CreateClient(_options, teamId);

            // get the existing shift from Teams
            var teamsShift = await client.GetShiftAsync(teamId, shift.TeamsShiftId).ConfigureAwait(false);

            teamsShift.SchedulingGroupId = shift.TeamsSchedulingGroupId;
            teamsShift.UserId = shift.TeamsEmployeeId;
            teamsShift.SharedShift = _shiftMap.MapShift(shift);

            var response = await client.ReplaceShiftAsync(teamsShift, teamId, teamsShift.Id).ConfigureAwait(false);
            response.ThrowIfError();
        }

        public async Task UpdateTimeOffAsync(string teamId, TimeOffModel timeOff)
        {
            Guard.ArgumentNotNullOrEmpty(timeOff.TeamsEmployeeId, nameof(ShiftModel.TeamsEmployeeId));

            var client = _clientFactory.CreateClient(_options, teamId);

            // get the existing time off from Teams
            var teamsTimeOff = await client.GetTimeOffAsync(teamId, timeOff.TeamsTimeOffId).ConfigureAwait(false);

            teamsTimeOff.UserId = timeOff.TeamsEmployeeId;
            teamsTimeOff.SharedTimeOff = _timeOffMap.MapTimeOff(timeOff);

            var response = await client.ReplaceTimeOffAsync(teamsTimeOff, teamId, teamsTimeOff.Id).ConfigureAwait(false);
            response.ThrowIfError();
        }

        private static AsyncRetryPolicy GetConflictRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<HttpOperationException>(h => h.Response.StatusCode == HttpStatusCode.Conflict)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryInterval));
        }

        private static AsyncRetryPolicy GetTimeoutRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<HttpOperationException>(h => (int)h.Response.StatusCode >= 400)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryInterval));
        }

        private bool AvailabilityMatches(IList<AvailabilityItem> existingAvailability, List<AvailabilityItem> defaultWeekAvailability)
        {
            foreach (var defaultItem in defaultWeekAvailability)
            {
                // find the matching day in the existing and compare
                var availabilityItem = existingAvailability.FirstOrDefault(a => a.Recurrence.Pattern.DaysOfWeek[0].Equals(defaultItem.Recurrence.Pattern.DaysOfWeek[0], StringComparison.OrdinalIgnoreCase));

                if (availabilityItem == null || availabilityItem.TimeSlots.Count != defaultItem.TimeSlots.Count)
                {
                    return false;
                }

                // there is only one timeslot in the default timeslots so we only need to compare
                // one value
                if (availabilityItem.TimeSlots[0].StartTime.Substring(0, 8) != defaultItem.TimeSlots[0].StartTime ||
                    availabilityItem.TimeSlots[0].EndTime.Substring(0, 8) != defaultItem.TimeSlots[0].EndTime)
                {
                    return false;
                }
            }

            return true;
        }

        private List<AvailabilityItem> CreateDefaultAvailabilityForWeek(string timeZone)
        {
            return new List<AvailabilityItem>
            {
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Sunday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Monday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Tuesday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Wednesday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Thursday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Friday), timeZone),
                CreateDefaultAvailabilityItemForDay(nameof(DayOfWeek.Saturday), timeZone)
            };
        }

        private AvailabilityItem CreateDefaultAvailabilityItemForDay(string dayOfWeek, string timeZone)
        {
            return new AvailabilityItem
            {
                Recurrence = new RecurrenceItem
                {
                    Pattern = new PatternItem
                    {
                        DaysOfWeek = new List<string>
                        {
                            dayOfWeek
                        },
                        Interval = 1,
                        Type = "Weekly"
                    },
                    Range = new RangeItem
                    {
                        Type = "noEnd"
                    }
                },
                TimeSlots = new List<TimeSlotItem>
                {
                    new TimeSlotItem
                    {
                        StartTime = "00:00:00",
                        EndTime = "00:00:00"
                    }
                },
                TimeZone = timeZone
            };
        }

        private async Task<string> GetSchedulingGroupIdByNameAsync(IMicrosoftGraphClient client, string teamId, string groupName)
        {
            // update the cache with the most current set of groups from teams
            var schedulingGroups = await RefreshCachedSchedulingGroupsAsync(client, teamId).ConfigureAwait(false);
            var group = schedulingGroups.Find(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            return group?.Id;
        }

        /// <summary>
        /// Gets the scheduling groups dictionary from cache if it is present there, otherwise gets
        /// the list from Teams
        /// </summary>
        /// <param name="client">The client used to communicate with Teams</param>
        /// <param name="teamId">The id of the team to get the scheduling groups for.</param>
        /// <returns></returns>
        private async Task<List<SchedulingGroupModel>> GetSchedulingGroupsAsync(IMicrosoftGraphClient client, string teamId)
        {
            // firstly get the scheduling groups from cache
            var schedulingGroups = await _cacheService.GetKeyAsync<List<SchedulingGroupModel>>(ApplicationConstants.TableNameGroupLists, teamId).ConfigureAwait(false);

            if (schedulingGroups == null || schedulingGroups.Count == 0)
            {
                schedulingGroups = await RefreshCachedSchedulingGroupsAsync(client, teamId).ConfigureAwait(false);
            }

            return schedulingGroups;
        }

        /// <summary>
        /// Refreshes the cached scheduling groups for the team from Teams itself.
        /// </summary>
        /// <param name="client">The client used to communicate with Teams</param>
        /// <param name="teamId">The id of the team to get the scheduling groups for.</param>
        /// <returns></returns>
        private async Task<List<SchedulingGroupModel>> RefreshCachedSchedulingGroupsAsync(IMicrosoftGraphClient client, string teamId)
        {
            var response = await client.ListSchedulingGroupsAsync(teamId).ConfigureAwait(false);

            var schedulingGroups = new List<SchedulingGroupModel>();
            foreach (var group in response.Value.Where(g => g.IsActive).OrderBy(g => g.CreatedDateTime).ThenBy(g => g.DisplayName))
            {
                schedulingGroups.Add(new SchedulingGroupModel
                {
                    Id = group.Id,
                    Name = group.DisplayName
                });
            }

            await _cacheService.SetKeyAsync(ApplicationConstants.TableNameGroupLists, teamId, schedulingGroups).ConfigureAwait(false);

            return schedulingGroups;
        }

        private void RemoveInvalidUsers(List<string> usersToValidate, List<string> teamMembers)
        {
            if (teamMembers.Count > 0)
            {
                usersToValidate.RemoveAll(uid => !teamMembers.Contains(uid));
            }
            else
            {
                // there are no users at all in Teams so remove all
                usersToValidate.Clear();
            }
        }

        private async Task RemoveUsersFromSchedulingGroupAsync(IMicrosoftGraphClient client, string teamId, string schedulingGroupId)
        {
            var group = await client.GetSchedulingGroupAsync(teamId, schedulingGroupId).ConfigureAwait(false);
            group.UserIds?.Clear();
            var response = await client.ReplaceSchedulingGroupAsync(group, group.Etag, teamId, group.Id).ConfigureAwait(false);
            response.ThrowIfError();
        }

        private async Task ReplaceSchedulingGroupAsync(IMicrosoftGraphClient client, string teamId, string teamsSchedulingGroupId, IList<string> userIds)
        {
            var teamMembers = await _cacheService.GetKeyAsync<List<string>>(ApplicationConstants.TableNameEmployeeLists, teamId).ConfigureAwait(false);
            var group = await client.GetSchedulingGroupAsync(teamId, teamsSchedulingGroupId).ConfigureAwait(false);
            var usersToAdd = userIds.Except(group.UserIds).ToList();

            if (usersToAdd.Count > 0)
            {
                // ensure that all users are actually in the Team and remove any that aren't
                RemoveInvalidUsers(usersToAdd, teamMembers);
                if (usersToAdd.Count == 0)
                {
                    return;
                }

                if (group.UserIds == null)
                {
                    group.UserIds = userIds;
                }
                else
                {
                    ((List<string>)group.UserIds).AddRange(usersToAdd);
                }

                await client.ReplaceSchedulingGroupAsync(group, group.Etag, teamId, teamsSchedulingGroupId).ConfigureAwait(false);
            }
        }

        private List<ShiftModel> MapToShiftModel(List<ShiftModel> shifts, ShiftCollectionResponse response)
        {
            var newShifts = response.Value
                .Select(s => new ShiftModel("")
                {
                    TeamsShiftId = s.Id,
                    TeamsEmployeeId = s.UserId,
                    TeamsSchedulingGroupId = s.SchedulingGroupId,
                    StartDate = s.SharedShift.StartDateTime.Value,
                    EndDate = s.SharedShift.EndDateTime.Value
                })
                .ToList();

            shifts.AddRange(newShifts);
            return shifts;
        }

        private List<ShiftModel> MapOpenShiftsToShiftModel(List<ShiftModel> shifts, OpenShiftCollectionResponse response)
        {
            var newOpenShifts = response.Value
                .Select(s => new ShiftModel("")
                {
                    TeamsShiftId = s.Id,
                    TeamsSchedulingGroupId = s.SchedulingGroupId,
                    StartDate = s.SharedOpenShift.StartDateTime,
                    EndDate = s.SharedOpenShift.EndDateTime,
                })
                .ToList();

            shifts.AddRange(newOpenShifts);
            return shifts;
        }

        private List<TimeOffModel> MapTimeOffToTimeOffModel(List<TimeOffModel> timeOff, TimeOffCollectionResponse response)
        {
            var newTimeOff = response.Value
                .Select(s => new TimeOffModel
                {
                    TeamsTimeOffId = s.Id,
                    TeamsTimeOffReasonId = s.SharedTimeOff.TimeOffReasonId,
                    TeamsEmployeeId = s.UserId,
                    StartDate = s.SharedTimeOff.StartDateTime,
                    EndDate = s.SharedTimeOff.EndDateTime
                })
                .ToList();

            timeOff.AddRange(newTimeOff);
            return timeOff;
        }
    }
}
