// ---------------------------------------------------------------------------
// <copyright file="TeamModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using WfmTeams.Adapter.Models;

    public class TeamModel
    {
        public string WfmBuId { get; set; }
        public string TeamId { get; set; }
        public string TimeZoneInfoId { get; set; }

        public static TeamModel FromConnection(ConnectionModel connectionModel)
        {
            return new TeamModel
            {
                TeamId = connectionModel.TeamId,
                WfmBuId = connectionModel.WfmBuId,
                TimeZoneInfoId = connectionModel.TimeZoneInfoId
            };
        }
    }
}
