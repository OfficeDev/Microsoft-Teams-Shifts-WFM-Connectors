using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Orchestrators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class ClearScheduleTrigger
    {
        [FunctionName(nameof(ClearScheduleTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "clearschedule")] ClearScheduleModel clearScheduleModel,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            if (!clearScheduleModel.IsValid())
            {
                return new BadRequestResult();
            }

            if (await starter.TryStartSingletonAsync(nameof(ClearScheduleOrchestrator), clearScheduleModel.InstanceId, clearScheduleModel))
            {
                return new OkResult();
            }
            else
            {
                return new ConflictResult();
            }
        }
    }
}
