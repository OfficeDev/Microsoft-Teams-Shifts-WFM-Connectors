// ---------------------------------------------------------------------------
// <copyright file="ConnectorHealthOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    public class ConnectorHealthOptions : TeamOrchestratorOptions
    {
        public string MissingTeamErrorCode { get; set; } = "Request_ResourceNotFound";
    }
}