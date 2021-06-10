using Microsoft.ApplicationInsights;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    /// <summary>
    /// Provides utility methods for working with users.
    /// </summary>
    internal static class UsersHelper
    {
        /// <summary>
        /// Get All mapped users, combining user and teams department mapping data.
        /// </summary>
        /// <param name="workForceIntegrationId">The workforce integration to get the users for.</param>
        /// <param name="userMappingProvider">The user mapping provider.</param>
        /// <param name="teamDepartmentMappingProvider">The team department mapping provider.</param>
        /// <param name="telemetryClient">The telemetry client for logging.</param>
        /// <returns>The full list of users.</returns>
        internal static async Task<IEnumerable<UserDetailsModel>> GetAllMappedUserDetailsAsync(string workForceIntegrationId, IUserMappingProvider userMappingProvider, ITeamDepartmentMappingProvider teamDepartmentMappingProvider, TelemetryClient telemetryClient)
        {
            Dictionary<string, TeamToDepartmentJobMappingEntity> teamMappingEntities = new Dictionary<string, TeamToDepartmentJobMappingEntity>();
            List<UserDetailsModel> kronosUsers = new List<UserDetailsModel>();

            List<AllUserMappingEntity> mappedUsersResult = await userMappingProvider.GetAllMappedUserDetailsAsync().ConfigureAwait(false);

            foreach (var element in mappedUsersResult)
            {
                TeamToDepartmentJobMappingEntity teamMappingEntity;
                var key = $"{workForceIntegrationId}_{element.PartitionKey}";
                if (teamMappingEntities.ContainsKey(key))
                {
                    teamMappingEntity = teamMappingEntities[key];
                }
                else
                {
                    teamMappingEntity = await teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(
                        workForceIntegrationId, element.PartitionKey).ConfigureAwait(false);
                    if (teamMappingEntity == null)
                    {
                        telemetryClient.TrackTrace($"Team {element.PartitionKey} not mapped.");
                        continue;
                    }

                    teamMappingEntities.Add(key, teamMappingEntity);
                }

                kronosUsers.Add(new UserDetailsModel
                {
                    KronosPersonNumber = element.RowKey,
                    ShiftUserId = element.ShiftUserAadObjectId,
                    ShiftTeamId = teamMappingEntity.TeamId,
                    ShiftScheduleGroupId = teamMappingEntity.TeamsScheduleGroupId,
                    OrgJobPath = element.PartitionKey,
                    ShiftUserDisplayName = element.ShiftUserDisplayName,
                    KronosTimeZone = teamMappingEntity.KronosTimeZone,
                });
            }

            return kronosUsers;
        }

        /// <summary>
        /// Get a single mapped user, combining user and teams department mapping data.
        /// </summary>
        /// <param name="workForceIntegrationId">The workforce integration to get the users for.</param>
        /// <param name="userId">The Teams id of the user to retrieve.</param>
        /// <param name="teamsId">The aadGroup Id.</param>
        /// <param name="userMappingProvider">The user mapping provider.</param>
        /// <param name="teamDepartmentMappingProvider">The team department mapping provider.</param>
        /// <param name="telemetryClient">The telemetry client for logging.</param>
        /// <returns>The full list of users.</returns>
        internal static async Task<UserDetailsModel> GetMappedUserDetailsAsync(string workForceIntegrationId, string userId, string teamsId, IUserMappingProvider userMappingProvider, ITeamDepartmentMappingProvider teamDepartmentMappingProvider, TelemetryClient telemetryClient)
        {
            var mappedUserResult = await userMappingProvider.GetUserMappingEntityAsyncNew(userId, teamsId).ConfigureAwait(false);

            var teamMappingEntity = await teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(workForceIntegrationId, mappedUserResult.PartitionKey).ConfigureAwait(false);
            if (teamMappingEntity == null)
            {
                telemetryClient.TrackTrace($"Team {mappedUserResult.PartitionKey} not mapped.");
            }

            return new UserDetailsModel
            {
                KronosPersonNumber = mappedUserResult.RowKey,
                ShiftUserId = mappedUserResult.ShiftUserAadObjectId,
                ShiftTeamId = teamMappingEntity.TeamId,
                ShiftScheduleGroupId = teamMappingEntity.TeamsScheduleGroupId,
                OrgJobPath = mappedUserResult.PartitionKey,
                ShiftUserDisplayName = mappedUserResult.ShiftUserDisplayName,
                KronosTimeZone = teamMappingEntity.KronosTimeZone,
            };
        }
    }
}