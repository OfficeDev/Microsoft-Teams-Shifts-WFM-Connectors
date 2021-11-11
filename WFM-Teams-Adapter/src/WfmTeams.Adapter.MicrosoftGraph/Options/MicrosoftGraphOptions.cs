// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Options
{
    using WfmTeams.Adapter.Options;

    public class MicrosoftGraphOptions : ConnectorOptions
    {
        public string AdminConsentUrl { get; set; } = "https://login.microsoftonline.com/common/adminconsent";
        public string AppScope { get; set; } = ".default";
        public string AppTokenUrl { get; set; } = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";
        public string AuthorizeUrl { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool IgnoreBreaks { get; set; } = true;
        public bool IgnoreMeals { get; set; } = true;
        public string MSBaseAddress { get; set; } = "https://graph.microsoft.com/v1.0";
        public string Scope { get; set; } = "Group.ReadWrite.All User.Read.All WorkforceIntegration.ReadWrite.All Schedule.ReadWrite.All";
        public string ShiftsAppUrl { get; set; }
        public string ThemeMap { get; set; }
        public string TimeOffTheme { get; set; } = "gray";
        public string TokenUrl { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        public string UserPrincipalNameFormatString { get; set; } = "{0}";
    }
}
