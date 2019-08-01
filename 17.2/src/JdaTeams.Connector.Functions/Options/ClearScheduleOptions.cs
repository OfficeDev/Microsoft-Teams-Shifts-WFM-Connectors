namespace JdaTeams.Connector.Functions.Options
{
    public class ClearScheduleOptions
    {
        public int ClearScheduleBatchSize { get; set; } = 50;
        public int ClearScheduleMaxBatchSize { get; set; } = 200;
        public int ClearScheduleMaxAttempts { get; set; } = 20;
    }
}
