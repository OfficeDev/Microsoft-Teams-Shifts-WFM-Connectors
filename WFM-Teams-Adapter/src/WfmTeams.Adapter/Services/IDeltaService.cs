// ---------------------------------------------------------------------------
// <copyright file="IDeltaService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using WfmTeams.Adapter.Models;

    public interface IDeltaService<T> where T : IDeltaItem
    {
        DeltaModel<T> ComputeDelta(IEnumerable<T> from, IEnumerable<T> to);
    }
}
