using JdaTeams.Connector.Options;
using JdaTeams.Connector.Services;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Helpers
{
    public class TimeZoneHelper : ITimeZoneHelper
    {
        private readonly ConnectorOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleSourceService _scheduleSourceService;

        public TimeZoneHelper(IScheduleSourceService scheduleSourceService, IScheduleConnectorService scheduleConnectorService, ConnectorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
        }

        public async Task<string> GetAndUpdateTimeZone(string teamId)
        {
            var timeZoneInfoId = _options.TimeZone;

            var connection = await _scheduleConnectorService.GetConnectionAsync(teamId);

            if (connection != null && !string.IsNullOrEmpty(connection.TimeZoneInfoId))
            {
                var store = await _scheduleSourceService.GetStoreAsync(connection.TeamId, connection.StoreId);

                if (store?.TimeZoneId != null)
                {
                    var jdaTimeZoneName = await _scheduleSourceService.GetJdaTimeZoneNameAsync(teamId, store.TimeZoneId.Value);
                    var returnedTimeZoneInfoId = await _scheduleConnectorService.GetTimeZoneInfoIdAsync(jdaTimeZoneName);

                    if (returnedTimeZoneInfoId != null)
                    {
                        connection.TimeZoneInfoId = returnedTimeZoneInfoId;
                        await _scheduleConnectorService.SaveConnectionAsync(connection);
                        timeZoneInfoId = returnedTimeZoneInfoId;
                    }
                }
            }

            return timeZoneInfoId;
        }

        public async Task<string> GetTimeZone(string teamId, int? timeZoneId)
        {
            if (timeZoneId.HasValue)
            {
                var jdaTimeZoneName = await _scheduleSourceService.GetJdaTimeZoneNameAsync(teamId, timeZoneId.Value);
                return await _scheduleConnectorService.GetTimeZoneInfoIdAsync(jdaTimeZoneName);
            }

            return null;
        }
    }
}