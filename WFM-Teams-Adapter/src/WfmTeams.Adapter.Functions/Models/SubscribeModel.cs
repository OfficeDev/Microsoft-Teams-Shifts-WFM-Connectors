// ---------------------------------------------------------------------------
// <copyright file="SubscribeModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System.ComponentModel.DataAnnotations;
    using WfmTeams.Adapter.Models;

    public class SubscribeModel
    {
        public string RedirectUri { get; set; }

        [Required]
        public string WfmBuId { get; set; }

        [Required]
        public string TeamId { get; set; }

        public ConnectionModel AsConnectionModel() => new ConnectionModel
        {
            TeamId = TeamId,
            WfmBuId = WfmBuId
        };

        public TeamModel AsTeamModel() => new TeamModel
        {
            WfmBuId = WfmBuId,
            TeamId = TeamId,
        };
    }
}
