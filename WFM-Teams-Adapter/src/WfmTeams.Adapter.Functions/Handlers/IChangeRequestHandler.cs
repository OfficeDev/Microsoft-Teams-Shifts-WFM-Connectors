// ---------------------------------------------------------------------------
// <copyright file="IChangeRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public interface IChangeRequestHandler
    {
        HandlerType ChangeHandlerType { get; }

        bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest);

        Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string entityId, ILogger log, IDurableOrchestrationClient starter);
    }
}
