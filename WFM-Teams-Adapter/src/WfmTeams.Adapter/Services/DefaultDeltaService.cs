// ---------------------------------------------------------------------------
// <copyright file="DefaultDeltaService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using WfmTeams.Adapter.Models;

    public abstract class DefaultDeltaService<T> : IDeltaService<T> where T : IDeltaItem
    {
        public DeltaModel<T> ComputeDelta(IEnumerable<T> from, IEnumerable<T> to)
        {
            var fromLookup = GetLookup(from);
            var toLookup = GetLookup(to);

            var createdKeys = toLookup.Keys.Except(fromLookup.Keys);
            var createdShifts = createdKeys.Select(k => toLookup[k]);

            var existingKeys = toLookup.Keys.Intersect(fromLookup.Keys);
            var existingShifts = existingKeys.Select(k => UpdateIdFields(fromLookup[k], toLookup[k])).ToArray();

            var updatedShifts = existingKeys
                .Where(k => HasChanges(fromLookup[k], toLookup[k]))
                .Select(k => toLookup[k]);

            var deletedShifts = fromLookup.Keys.Except(toLookup.Keys)
                .Select(k => fromLookup[k]);

            return new DeltaModel<T>(createdShifts, updatedShifts, deletedShifts);
        }

        protected abstract Dictionary<string, T> GetLookup(IEnumerable<T> list);

        protected abstract bool HasChanges(T from, T to);

        protected abstract T UpdateIdFields(T from, T to);
    }
}
