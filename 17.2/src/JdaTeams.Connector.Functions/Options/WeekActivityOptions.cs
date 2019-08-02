using JdaTeams.Connector.Options;

namespace JdaTeams.Connector.Functions.Options
{
    public class WeekActivityOptions : ConnectorOptions
    {
        public bool NotifyTeamOnChange { get; set; } = false;
        public int MaximumDelta { get; set; } = 200;
    }
}
