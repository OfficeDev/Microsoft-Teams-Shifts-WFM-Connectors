using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class PurgeTrigger
    {
        [FunctionName(nameof(PurgeTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "purge")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            var runtimeStatus = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.ContinuedAsNew
            };
            var instances = await starter.GetStatusAsync(DateTime.MinValue, DateTime.MaxValue, runtimeStatus);

            foreach (var instance in instances)
            {
                await starter.TerminateAsync(instance.InstanceId, nameof(PurgeTrigger));
            }

            return new OkResult();
        }
    }
}
