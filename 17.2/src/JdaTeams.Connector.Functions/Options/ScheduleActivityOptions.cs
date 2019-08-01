using System;

namespace JdaTeams.Connector.Functions.Options
{
    public class ScheduleActivityOptions
    {
        public int PollIntervalSeconds { get; set; } = 10;
        public int PollMaxAttempts { get; set; } = 20;
        public string TimeZone { get; set; } = "Europe/London";

        public TimeSpan AsPollIntervalTimeSpan()
        {
            return TimeSpan.FromSeconds(PollIntervalSeconds);
        }
    }
}
