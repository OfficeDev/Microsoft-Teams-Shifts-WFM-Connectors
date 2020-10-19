using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Activities;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Options;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class WeekOrchestrator
    {
        private readonly ConnectorOptions _options;

        public WeekOrchestrator(ConnectorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(WeekOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var weekModel = context.GetInput<WeekModel>();

            var resultModel = new ResultModel();

            do
            {
                var weekResultModel = await context.CallActivityAsync<ResultModel>(nameof(WeekActivity), weekModel);
                resultModel.AddResult(weekResultModel);
            }
            while (!resultModel.Finished);

            if (_options.DraftShiftsEnabled && resultModel.CreatedCount > 0)
            {
                var shareModel = new ShareModel
                {
                    TeamId = weekModel.TeamId,
                    StartDate = weekModel.StartDate,
                    EndDate = weekModel.StartDate.AddWeek(),
                    TimeZoneInfoId = weekModel.TimeZoneInfoId
                };
                await context.CallActivityAsync(nameof(ShareActivity), shareModel);
            }
        }
    }
}
