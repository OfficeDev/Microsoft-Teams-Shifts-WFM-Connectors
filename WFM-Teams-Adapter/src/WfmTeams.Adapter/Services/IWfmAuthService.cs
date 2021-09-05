// ---------------------------------------------------------------------------
// <copyright file="IWfmAuthService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;

    public interface IWfmAuthService
    {
        IActionResult HandleFederatedAuthRequest(HttpRequest request, ILogger log);

        Task RefreshEmployeeTokenAsync(EmployeeModel employee, string wfmBuId, ILogger log);
    }
}
