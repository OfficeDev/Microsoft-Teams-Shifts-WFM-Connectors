using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class StopTrigger
    {
        [FunctionName(nameof(StopTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "stop/{teamId}")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            await starter.TerminateAsync(teamId, nameof(StopTrigger));

            return new OkResult();
        }
    }
}
