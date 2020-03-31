using Microsoft.Extensions.Logging;

namespace JdaTeams.Connector.Models
{
    public class ResultModel
    {
        public ResultModel()
        {

        }

        public ResultModel(bool finished)
        {
            Finished = finished;
        }

        public bool Finished { get; set; }

        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int DeletedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public int IterationCount { get; set; }

        public void AddResult(ResultModel resultModel)
        {
            CreatedCount += resultModel.CreatedCount;
            UpdatedCount += resultModel.UpdatedCount;
            DeletedCount += resultModel.DeletedCount;
            FailedCount += resultModel.FailedCount;
            SkippedCount += resultModel.SkippedCount;
            IterationCount += resultModel.IterationCount;
            Finished = resultModel.Finished;
        }

        public LogLevel LogLevel => !Finished || FailedCount > 0 ? LogLevel.Warning : LogLevel.Information;
    }
}
