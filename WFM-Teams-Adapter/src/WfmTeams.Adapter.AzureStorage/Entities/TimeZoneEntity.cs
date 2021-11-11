// ---------------------------------------------------------------------------
// <copyright file="TimeZoneEntity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Entities
{
    using Microsoft.Azure.Cosmos.Table;

    public class TimeZoneEntity : TableEntity
    {
        public const string DefaultPartitionKey = "timezones";

        public TimeZoneEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public string TimeZoneInfoId { get; set; }
    }
}
