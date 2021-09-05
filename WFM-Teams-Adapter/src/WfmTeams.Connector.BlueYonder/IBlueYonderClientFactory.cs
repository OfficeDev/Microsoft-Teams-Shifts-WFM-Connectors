// ---------------------------------------------------------------------------
// <copyright file="IBlueYonderClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder
{
    using WfmTeams.Adapter.Models;

    public interface IBlueYonderClientFactory
    {
        IBlueYonderClient CreatePublicClient(CredentialsModel credentialsModel, string principalId, string apiPath);

        IBlueYonderClient CreateUserClient(CredentialsModel credentialsModel, string principalId, string apiPath);
    }
}
