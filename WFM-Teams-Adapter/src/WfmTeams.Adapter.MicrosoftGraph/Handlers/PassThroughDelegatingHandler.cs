// ---------------------------------------------------------------------------
// <copyright file="PassThroughDelegatingHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public class PassThroughDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // add the passthrough header to ensure that our change endpoint knows that the change
            // is a result of something we have initiated and therefore should be ignored - just
            // make sure we don't add it twice
            if (request.Headers.Contains(ChangeRequest.MSPassthroughRequestHeader))
            {
                request.Headers.Remove(ChangeRequest.MSPassthroughRequestHeader);
            }
            request.Headers.Add(ChangeRequest.MSPassthroughRequestHeader, ChangeRequest.PassThroughName);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
