// ---------------------------------------------------------------------------
// <copyright file="ConnectionEntity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Entities
{
    using System;
    using Microsoft.Azure.Cosmos.Table;
    using WfmTeams.Adapter.Models;

    public class ConnectionEntity : TableEntity
    {
        public const string DefaultPartitionKey = "teams";

        public ConnectionEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public bool Enabled { get; set; } = true;
        public DateTime? LastAOExecution { get; set; }
        public DateTime? LastECOExecution { get; set; }
        public DateTime? LastETROExecution { get; set; }
        public DateTime? LastOSOExecution { get; set; }
        public DateTime? LastSOExecution { get; set; }
        public DateTime? LastTOOExecution { get; set; }
        public string WfmBuId { get; set; }
        public string WfmBuName { get; set; }
        public string TeamName { get; set; }

        public string TimeZoneInfoId { get; set; }

        public static ConnectionEntity FromId(string teamId)
        {
            return new ConnectionEntity
            {
                RowKey = teamId,
                ETag = "*"
            };
        }

        public static ConnectionEntity FromModel(ConnectionModel model)
        {
            return new ConnectionEntity
            {
                RowKey = model.TeamId,
                WfmBuId = model.WfmBuId,
                WfmBuName = model.WfmBuName,
                TeamName = model.TeamName,
                LastAOExecution = model.LastAOExecution,
                LastECOExecution = model.LastECOExecution,
                LastETROExecution = model.LastETROExecution,
                LastOSOExecution = model.LastOSOExecution,
                LastSOExecution = model.LastSOExecution,
                LastTOOExecution = model.LastTOOExecution,
                Enabled = model.Enabled,
                TimeZoneInfoId = model.TimeZoneInfoId
            };
        }

        public ConnectionModel AsModel()
        {
            return new ConnectionModel
            {
                TeamId = RowKey,
                WfmBuId = WfmBuId,
                WfmBuName = WfmBuName,
                TeamName = TeamName,
                LastAOExecution = LastAOExecution,
                LastECOExecution = LastECOExecution,
                LastETROExecution = LastETROExecution,
                LastOSOExecution = LastOSOExecution,
                LastSOExecution = LastSOExecution,
                LastTOOExecution = LastTOOExecution,
                Enabled = Enabled,
                TimeZoneInfoId = TimeZoneInfoId
            };
        }
    }
}
