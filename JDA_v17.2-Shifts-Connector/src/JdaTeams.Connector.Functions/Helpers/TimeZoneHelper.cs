using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Options;
using JdaTeams.Connector.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Helpers
{
    public static class TimeZoneHelper
    {
        public static async Task<string> GetAndUpdateTimeZoneAsync(string teamId, IScheduleConnectorService scheduleConnectorService, IScheduleSourceService scheduleSourceService)
        {
            var connection = await scheduleConnectorService.GetConnectionAsync(teamId).ConfigureAwait(false);
            if (connection == null)
            {
                // the team must have been unsubscribed
                throw new ArgumentException("This team cannot be found in the teams table.", teamId);
            }

            if (!string.IsNullOrEmpty(connection.TimeZoneInfoId))
            {
                return connection.TimeZoneInfoId;
            }
            else
            {
                // we don't have a timezone for this existing connection, so try to get one
                var store = await scheduleSourceService.GetStoreAsync(connection.TeamId, connection.StoreId).ConfigureAwait(false);

                if (store?.TimeZoneId != null)
                {
                    var jdaTimeZoneName = await scheduleSourceService.GetJdaTimeZoneNameAsync(teamId, store.TimeZoneId.Value).ConfigureAwait(false);
                    var timeZoneInfoId = await scheduleConnectorService.GetTimeZoneInfoIdAsync(jdaTimeZoneName).ConfigureAwait(false);

                    if (timeZoneInfoId != null)
                    {
                        connection.TimeZoneInfoId = timeZoneInfoId;
                        await scheduleConnectorService.SaveConnectionAsync(connection).ConfigureAwait(false);
                        return timeZoneInfoId;
                    }
                }
            }

            return null;
        }

        public static async Task<string> GetTimeZoneAsync(string teamId, int? timeZoneId, IScheduleSourceService scheduleSourceService, IScheduleConnectorService scheduleConnectorService, ILogger log)
        {
            if (timeZoneId.HasValue)
            {
                var jdaTimeZoneName = await scheduleSourceService.GetJdaTimeZoneNameAsync(teamId, timeZoneId.Value).ConfigureAwait(false);
                try
                {
                    return await scheduleConnectorService.GetTimeZoneInfoIdAsync(jdaTimeZoneName).ConfigureAwait(false);
                }
                catch(KeyNotFoundException ex)
                {
                    // the BY time zone name was not found in the map table so just return null
                    log.LogTimeZoneError(ex, teamId, timeZoneId.Value, jdaTimeZoneName);
                }
            }

            return null;
        }
    }
}