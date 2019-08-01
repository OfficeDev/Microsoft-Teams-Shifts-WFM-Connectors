using JdaTeams.Connector.Extensions;
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

        public ShareActivity(WeekActivityOptions options, IScheduleDestinationService scheduleDestinationService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
        }

        [FunctionName(nameof(ShareActivity))]
        public async Task Run([ActivityTrigger] ShareModel shareModel, ILogger log)
        {
            // adjust the start and end dates for timezone information to ensure that all shifts actually modified in the period are shared
            var startDate = shareModel.StartDate.ApplyTimeZoneOffset(_options.TimeZone);
            var endDate = shareModel.EndDate.ApplyTimeZoneOffset(_options.TimeZone);
            await _scheduleDestinationService.ShareScheduleAsync(shareModel.TeamId, startDate, endDate, _options.NotifyTeamOnChange);
        }
    }
}
