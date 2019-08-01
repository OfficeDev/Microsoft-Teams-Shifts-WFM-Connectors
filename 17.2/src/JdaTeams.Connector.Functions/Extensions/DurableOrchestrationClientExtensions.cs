using Microsoft.Azure.WebJobs;
using System.Linq;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Extensions
{
    public static class DurableOrchestrationClientExtensions
    {
        public static async Task<bool> TryStartSingletonAsync(this DurableOrchestrationClient starter, string orchestrationName, string instanceId, object input)
        {
            var inactiveStatuses = new OrchestrationRuntimeStatus[]
            {
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Completed
            };
            var instance = await starter.GetStatusAsync(instanceId);

            if (instance == null || inactiveStatuses.Contains(instance.RuntimeStatus))
            {
                await starter.StartNewAsync(orchestrationName, instanceId, input);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
