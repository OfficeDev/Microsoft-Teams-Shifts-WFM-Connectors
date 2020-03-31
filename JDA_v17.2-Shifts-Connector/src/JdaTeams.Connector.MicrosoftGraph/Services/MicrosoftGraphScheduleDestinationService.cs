using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Extensions;
using JdaTeams.Connector.MicrosoftGraph.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Models;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Rest;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace JdaTeams.Connector.MicrosoftGraph.Services
{
    public class MicrosoftGraphScheduleDestinationService : IScheduleDestinationService
    {
        private readonly MicrosoftGraphOptions _options;
        private readonly IMicrosoftGraphClientFactory _clientFactory;
        private readonly IUserPrincipalMap _userPrincipalMap;
        private readonly IShiftMap _shiftMap;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _schedulingGroups = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EmployeeModel>> _teamMembers = new ConcurrentDictionary<string, ConcurrentDictionary<string, EmployeeModel>>();

        public MicrosoftGraphScheduleDestinationService(MicrosoftGraphOptions options, IMicrosoftGraphClientFactory clientFactory, IUserPrincipalMap userPrincipalMap, IShiftMap shiftMap)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _userPrincipalMap = userPrincipalMap ?? throw new ArgumentNullException(nameof(userPrincipalMap));
            _shiftMap = shiftMap ?? throw new ArgumentNullException(nameof(shiftMap));
        }

        public async Task<EmployeeModel> GetEmployeeAsync(string teamId, string login)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            await LoadMembersAsync(client, teamId);

            return _userPrincipalMap.MapEmployee(login, _teamMembers[teamId]);
        }

        public async Task<string> GetSchedulingGroupIdByNameAsync(string teamId, string groupName)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var groups = await GetSchedulingGroupsAsync(client, teamId);

            return groups.GetValueOrDefault(groupName);
        }

        public async Task<string> CreateSchedulingGroupAsync(string teamId, string groupName, List<string> userIds)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // ensure that all users are actually in the Team and remove any that aren't
            var teamMembers = await GetMemberIdsAsync(client, teamId);
            RemoveInvalidUsers(userIds, teamMembers);
            if (userIds.Count == 0)
            {
                return null;
            }

            // because we are caching the scheduling groups we cannot be certain that a group has not been
            // created by another instance of the service, so we should double-check prior to creating the
            // group
            var groupId = await GetSchedulingGroupIdByNameAsync(client, teamId, groupName);
            if (string.IsNullOrEmpty(groupId))
            {
                var request = new SchedulingGroupRequest
                {
                    DisplayName = groupName,
                    IsActive = true,
                    UserIds = userIds
                };

                var response = await client.CreateSchedulingGroupAsync(request, teamId);
                response.ThrowIfError();

                groupId = ((SchedulingGroupResponse)response).Id;

                // update the cache of scheduling groups
                var groups = await GetSchedulingGroupsAsync(client, teamId);
                groups[groupName] = groupId;
            }

            return groupId;
        }

        public async Task AddUsersToSchedulingGroupAsync(string teamId, string teamsSchedulingGroupId, List<string> userIds)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var policy = GetConflictRetryPolicy(_options.RetryMaxAttempts, _options.RetryIntervalSeconds);
            await policy.ExecuteAsync(() => ReplaceSchedulingGroupAsync(client, teamId, teamsSchedulingGroupId, userIds));
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

            var response = await client.CreateShiftAsync(request, teamId);
            response.ThrowIfError();

            // update the shift with the Teams id for the shift
            shift.TeamsShiftId = ((ShiftResponse)response).Id;
        }

        public async Task DeleteShiftAsync(string teamId, ShiftModel shift)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            await client.DeleteShiftAsync(teamId, shift.TeamsShiftId);
        }

        public async Task UpdateShiftAsync(string teamId, ShiftModel shift)
        {
            Guard.ArgumentNotNullOrEmpty(shift.TeamsSchedulingGroupId, nameof(ShiftModel.TeamsSchedulingGroupId));
            Guard.ArgumentNotNullOrEmpty(shift.TeamsEmployeeId, nameof(ShiftModel.TeamsEmployeeId));

            var client = _clientFactory.CreateClient(_options, teamId);

            // get the existing shift from Teams
            var teamsShift = client.GetShift(teamId, shift.TeamsShiftId);

            teamsShift.SchedulingGroupId = shift.TeamsSchedulingGroupId;
            teamsShift.UserId = shift.TeamsEmployeeId;
            teamsShift.SharedShift = _shiftMap.MapShift(shift);
            var response = await client.ReplaceShiftAsync(teamsShift, teamId, teamsShift.Id);
            response.ThrowIfError();
        }

        public async Task<List<ShiftModel>> ListShiftsAsync(string teamId, DateTime startDate, DateTime endDate, int maxNumber)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            // get a list of shifts for the date range
            var filter = $"sharedShift/startDateTime ge {startDate.ToString("o")} and sharedShift/endDateTime le {endDate.ToString("o")}";
            var response = await client.ListShiftsAsync(filter, teamId, maxNumber);

            return response.Value
                .Select(s => new ShiftModel("")
                {
                    TeamsShiftId = s.Id,
                    TeamsEmployeeId = s.UserId,
                    TeamsSchedulingGroupId = s.SchedulingGroupId,
                    StartDate = s.SharedShift.StartDateTime.Value,
                    EndDate = s.SharedShift.EndDateTime.Value
                })
                .ToList();
        }

        private async Task<string> GetSchedulingGroupIdByNameAsync(IMicrosoftGraphClient client, string teamId, string groupName)
        {
            // check the cached active groups first
            var groups = await GetSchedulingGroupsAsync(client, teamId);
            if (groups.ContainsKey(groupName))
            {
                return groups[groupName];
            }

            var response = await client.ListSchedulingGroupsAsync(teamId);
            var group = response.Value.Where(g => g.IsActive && g.DisplayName.Equals(groupName, StringComparison.OrdinalIgnoreCase)).OrderByDescending(g => g.CreatedDateTime).FirstOrDefault();

            if (group != null)
            {
                // we have found the group in teams, so update the cache while we are here
                groups[groupName] = group.Id;
                return group.Id;
            }

            return null;
        }

        private async Task<ConcurrentDictionary<string, string>> GetSchedulingGroupsAsync(IMicrosoftGraphClient client, string teamId)
        {
            if (!_schedulingGroups.ContainsKey(teamId))
            {
                var response = await client.ListSchedulingGroupsAsync(teamId);

                var concurrentGroups = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var groupList = response.Value.Where(g => g.IsActive).OrderBy(g => g.CreatedDateTime).ThenBy(g => g.DisplayName);
                foreach (var group in groupList)
                {
                    concurrentGroups[group.DisplayName] = group.Id;
                }
                _schedulingGroups[teamId] = concurrentGroups;
            }

            return _schedulingGroups[teamId];
        }

        public async Task<ScheduleModel> GetScheduleAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var scheduleResponse = await client.GetScheduleAsync(teamId);
            return new ScheduleModel
            {
                IsEnabled = scheduleResponse.Enabled,
                Status = scheduleResponse.ProvisionStatus,
                TimeZone = scheduleResponse.TimeZone
            };
        }

        public async Task CreateScheduleAsync(string teamId, ScheduleModel schedule)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var scheduleRequest = new ScheduleRequest
            {
                Enabled = schedule.IsEnabled,
                TimeZone = schedule.TimeZone
            };
            await client.CreateReplaceScheduleAsync(scheduleRequest, teamId);
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
            await policy.ExecuteAsync(() => client.ShareScheduleAsync(shareRequest, teamId));
        }

        private static AsyncRetryPolicy GetTimeoutRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<HttpOperationException>(h => (int)h.Response.StatusCode >= 400)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryInterval));
        }

        private static AsyncRetryPolicy GetConflictRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<HttpOperationException>(h => h.Response.StatusCode == HttpStatusCode.Conflict)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryInterval));
        }

        private async Task ReplaceSchedulingGroupAsync(IMicrosoftGraphClient client, string teamId, string teamsSchedulingGroupId, IList<string> userIds)
        {
            // to ensure that we minimise concurrency issues, we should ensure we have populated
            // the members cache first
            var teamMembers = await GetMemberIdsAsync(client, teamId);
            var group = await client.GetSchedulingGroupAsync(teamId, teamsSchedulingGroupId);
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

                await client.ReplaceSchedulingGroupAsync(group, group.Etag, teamId, teamsSchedulingGroupId);
            }
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

        private async Task LoadMembersAsync(IMicrosoftGraphClient client, string teamId)
        {
            if (!_teamMembers.ContainsKey(teamId))
            {
                // get the list of members of the team from teams - 999 is the maximum that can be returned in a single call
                var response = await client.GetMembersAsync(teamId, "id,displayName,userPrincipalName", 999);
                var employees = response.Value
                    .Select(m => new EmployeeModel
                    {
                        DestinationId = m.Id,
                        DisplayName = m.DisplayName,
                        LoginName = m.UserPrincipalName
                    });

                 _teamMembers.GetValueOrCreate(teamId)
                    .AddRange(employees, e => e.LoginName);
            }
        }

        private async Task<List<string>> GetMemberIdsAsync(IMicrosoftGraphClient client, string teamId)
        {
            await LoadMembersAsync(client, teamId);

            return _teamMembers[teamId]
                .Select(m => m.Value.DestinationId)
                .ToList();
        }

        public async Task<List<string>> ListActiveSchedulingGroupIdsAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var response = await client.ListSchedulingGroupsAsync(teamId);

            return response.Value
                .Where(i => i.IsActive)
                .Select(i => i.Id)
                .ToList();
        }

        public Task RemoveUsersFromSchedulingGroupAsync(string teamId, string schedulingGroupId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);
            var policy = GetConflictRetryPolicy(_options.RetryMaxAttempts, _options.RetryIntervalSeconds);

            return policy.ExecuteAsync(async () =>
            {
                await RemoveUsersFromSchedulingGroupAsync(client, teamId, schedulingGroupId);
            });
        }

        private async Task RemoveUsersFromSchedulingGroupAsync(IMicrosoftGraphClient client, string teamId, string schedulingGroupId)
        {
            var group = await client.GetSchedulingGroupAsync(teamId, schedulingGroupId);
            group.UserIds?.Clear();
            var response = await client.ReplaceSchedulingGroupAsync(group, group.Etag, teamId, group.Id);
            response.ThrowIfError();
        }

        public async Task<GroupModel> GetTeamAsync(string teamId)
        {
            var client = _clientFactory.CreateClient(_options, teamId);

            var team = await client.GetTeamAsync(teamId, "id,displayName,createdDateTime,deletedDateTime");

            return new GroupModel
            {
                Id = team.Id,
                Name = team.DisplayName,
                CreatedDateTime = team.CreatedDateTime,
                DeletedDateTime = team.DeletedDateTime
            };
        }
    }
}
