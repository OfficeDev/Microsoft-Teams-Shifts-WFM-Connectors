using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.Options
{
    public class ConnectorOptions
    {
        public int RetryIntervalSeconds { get; set; } = 5;
        public int RetryMaxAttempts { get; set; } = 5;
        public int LongOperationRetryIntervalSeconds { get; set; } = 15;
        public int LongOperationMaxAttempts { get; set; } = 8;
        public bool DraftShiftsEnabled { get; set; } = false;
        public string TimeZone { get; set; } = "GMT Standard Time";
        public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Monday;

    }
}
