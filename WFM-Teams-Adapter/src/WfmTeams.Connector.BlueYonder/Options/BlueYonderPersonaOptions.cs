// ---------------------------------------------------------------------------
// <copyright file="BlueYonderPersonaOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Options
{
    using Microsoft.IdentityModel.Tokens;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Options;

    public class BlueYonderPersonaOptions : ConnectorOptions
    {
        public string FederatedAuthBaseAddress { get; set; }
        public string FederatedAuthTokenAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256;
        public int FederatedAuthTokenExpiration { get; set; } = 120;
        public string FederatedAuthTokenIssuer { get; set; } = "wfmteams.authenticator";
        public string FederatedAuthTokenName { get; set; } = "X-MS-AuthToken";
        public string FederatedAuthTokenSigningSecret { get; set; }
        public string RetailWebApiPath { get; set; } = "/data/retailwebapi/api/v1";
        public string BlueYonderBaseAddress { get; set; }
        public string BlueYonderCookieAuthPath { get; set; } = "/data/login";
        public int BlueYonderCookieDelayMs { get; set; } = 1000;
        public string SuperUserPassword { get; set; }
        public string SuperUserUsername { get; set; }
        public int StoreManagerSecurityGroupId { get; set; }
        public string TimeOffReasonWhitelist { get; set; }
        public string EssApiPath { get; set; } = "/data/wfmess/api/v1-beta1";
        public string SiteManagerApiPath { get; set; } = "/data/wfmsm/api/v1-beta2";

        public CredentialsModel AsCredentials()
        {
            return new CredentialsModel
            {
                BaseAddress = BlueYonderBaseAddress,
                Username = SuperUserUsername,
                Password = SuperUserPassword
            };
        }
    }
}
