using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Activities
{
    public class ShareActivity
    {
        private readonly WeekActivityOptions _options;
        private readonly IScheduleDestinationService _scheduleDestinationService;
        private readonly ITimeZoneHelper _timeZoneHelper;

        public ShareActivity(WeekActivityOptions options, IScheduleDestinationService scheduleDestinationService, ITimeZoneHelper timeZoneHelper)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
            _timeZoneHelper = timeZoneHelper ?? throw new ArgumentNullException(nameof(timeZoneHelper));
        }

        [FunctionName(nameof(ShareActivity))]
        public async Task Run([ActivityTrigger] ShareModel shareModel, ILogger log)
        {
            // adjust the start and end dates for TimeZone information to ensure that all shifts actually modified in the period are shared
            var timeZoneInfo = await _timeZoneHelper.GetAndUpdateTimeZone(shareModel.TeamId);
            var startDate = shareModel.StartDate.ApplyTimeZoneOffset(timeZoneInfo);
            var endDate = shareModel.EndDate.ApplyTimeZoneOffset(timeZoneInfo);
            await _scheduleDestinationService.ShareScheduleAsync(shareModel.TeamId, startDate, endDate, _options.NotifyTeamOnChange);
        }
    }
}
