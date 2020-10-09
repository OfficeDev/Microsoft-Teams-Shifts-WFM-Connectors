using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace JdaTeams.Connector.Functions.Activities
{
    public class ScheduleActivity
    {
        private readonly ScheduleActivityOptions _options;
        private readonly IScheduleDestinationService _scheduleDestinationService;
        private readonly ITimeZoneHelper _timeZoneHelper;

        public ScheduleActivity(ScheduleActivityOptions options, IScheduleDestinationService scheduleDestinationService, ITimeZoneHelper timeZoneHelper)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
            _timeZoneHelper = timeZoneHelper ?? throw new ArgumentException(nameof(timeZoneHelper));
        }

        [FunctionName(nameof(ScheduleActivity))]
        public async Task Run([ActivityTrigger] TeamModel teamModel, ILogger log)
        {
            var schedule = await _scheduleDestinationService.GetScheduleAsync(teamModel.TeamId);

            if (schedule.IsUnavailable)
            {
                var timeZoneInfo = await _timeZoneHelper.GetAndUpdateTimeZone(teamModel.TeamId);

                var scheduleModel = ScheduleModel.Create(TZConvert.WindowsToIana(timeZoneInfo));
                
                await _scheduleDestinationService.CreateScheduleAsync(teamModel.TeamId, scheduleModel);
            }
            else if (schedule.IsProvisioned)
            {
                log.LogSchedule(teamModel, schedule);

                return;
            }

            for (var i = 0; i < _options.PollMaxAttempts; i++)
            {
                await Task.Delay(_options.AsPollIntervalTimeSpan());

                schedule = await _scheduleDestinationService.GetScheduleAsync(teamModel.TeamId);

                log.LogSchedule(teamModel, schedule);

                if (schedule.IsProvisioned)
                {
                    return;
                }
            }

            throw new TimeoutException();
        }
    }
}
