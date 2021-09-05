// ---------------------------------------------------------------------------
// <copyright file="GroupModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    /// <summary>
    /// Defines the model for a group (team in Microsoft Teams).
    /// </summary>
    public class GroupModel
    {
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
