using JdaTeams.Connector.Functions.Activities;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class ClearShiftsDayOrchestrator
    {
        private readonly ClearScheduleOptions _options;

        public ClearShiftsDayOrchestrator(ClearScheduleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(ClearShiftsDayOrchestrator))]
        public async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var clearScheduleModel = context.GetInput<ClearScheduleModel>();

            log.LogClearStart(clearScheduleModel);

            var resultModel = new ResultModel();

            do
            {
                var activityResultModel = await context.CallActivityAsync<ResultModel>(nameof(ClearShiftsActivity), clearScheduleModel);

                resultModel.AddResult(activityResultModel);
            }
            while (!resultModel.Finished && ++resultModel.IterationCount < _options.ClearScheduleMaxAttempts);

            log.LogClearEnd(clearScheduleModel, resultModel);
        }
    }
}