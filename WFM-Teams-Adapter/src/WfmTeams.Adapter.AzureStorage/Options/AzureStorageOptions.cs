// ---------------------------------------------------------------------------
// <copyright file="AzureStorageOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Options
{
    public class AzureStorageOptions
    {
        public string AppBlobName { get; set; } = "index.html";
        public string AppContainerName { get; set; } = "app";
        public string ConnectionString { get; set; }
        public string CredentialsTableName { get; set; } = "credentials";
        public string RequestsContainerName { get; set; } = "requests";
        public string ShiftsContainerName { get; set; } = "shifts";
        public int TakeCount { get; set; } = 1000;
        public string TeamTableName { get; set; } = "teams";
        public string TimeOffContainerName { get; set; } = "timeoff";
        public string TokensTableName { get; set; } = "tokens";
        public string TimeZoneTableName { get; set; } = "timezones";
    }
}
