// ---------------------------------------------------------------------------
// <copyright file="IMicrosoftGraphClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph
{
    using WfmTeams.Adapter.MicrosoftGraph.Options;

    public interface IMicrosoftGraphClientFactory
    {
        IMicrosoftGraphClient CreateUserClient(MicrosoftGraphOptions options, string userId);

        IMicrosoftGraphClient CreateClient(MicrosoftGraphOptions options, string teamId);

        IMicrosoftGraphClient CreateClientNoPassthrough(MicrosoftGraphOptions options, string teamId);
    }
}
