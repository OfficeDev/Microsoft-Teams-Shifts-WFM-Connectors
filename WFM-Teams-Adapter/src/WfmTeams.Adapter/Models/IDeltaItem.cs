// ---------------------------------------------------------------------------
// <copyright file="IDeltaItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    /// <summary>
    /// Defines the interface that must be implemented by any entity being synced.
    /// </summary>
    public interface IDeltaItem
    {
        string WfmEmployeeId { get; set; }
        string WfmId { get; }
        string TeamsEmployeeId { get; set; }
    }
}
