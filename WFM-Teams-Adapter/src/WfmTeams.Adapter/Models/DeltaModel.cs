// ---------------------------------------------------------------------------
// <copyright file="DeltaModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The model defining the computed delta for each sync activity.
    /// </summary>
    /// <typeparam name="T">The type of entity being synced e.g. shift, open shift etc.</typeparam>
    public class DeltaModel<T> where T : IDeltaItem
    {
        private readonly ConcurrentDictionary<T, object> _created = new ConcurrentDictionary<T, object>();

        private readonly ConcurrentDictionary<T, object> _deleted = new ConcurrentDictionary<T, object>();

        private readonly ConcurrentDictionary<T, object> _failed = new ConcurrentDictionary<T, object>();

        private readonly ConcurrentDictionary<T, object> _skipped = new ConcurrentDictionary<T, object>();

        private readonly ConcurrentDictionary<T, object> _updated = new ConcurrentDictionary<T, object>();

        public DeltaModel(IEnumerable<T> created, IEnumerable<T> updated, IEnumerable<T> deleted)
        {
            foreach (T item in created)
            {
                _created[item] = item;
            }

            foreach (T item in updated)
            {
                _updated[item] = item;
            }

            foreach (T item in deleted)
            {
                _deleted[item] = item;
            }
        }

        public List<T> All => Created.Union(Updated).Union(Deleted).ToList();
        public ICollection<T> Created => _created.Keys;
        public ICollection<T> Deleted => _deleted.Keys;
        public ICollection<T> Failed => _failed.Keys;
        public bool HasChanges => Created.Any() || Updated.Any() || Deleted.Any();
        public bool HasContinuation { get; private set; }
        public ICollection<T> Skipped => _skipped.Keys;
        public ICollection<T> Updated => _updated.Keys;

        public void ApplyChanges(List<T> items)
        {
            // ideally the cache should not contain any of the created shifts, however, we have
            // observed in the wild that there is some scenario which we have not got to the bottom
            // of where it can, so we handle it here by removing any created shifts as well
            // TODO: we can remove Created from this once the underlying issue with duplicate shifts is found and fixed
            var changedIds = Created.Union(Updated).Union(Deleted).Select(u => u.WfmId).ToArray();

            items.RemoveAll(s => changedIds.Contains(s.WfmId));
            items.AddRange(Created);
            items.AddRange(Updated);
        }

        public void ApplyMaximum(int maximumDelta)
        {
            var continuation = All
                .Skip(maximumDelta)
                .ToArray();

            if (continuation.Any())
            {
                HasContinuation = true;

                foreach (var model in continuation)
                {
                    RemoveChange(model);
                }
            }
        }

        public void ApplySkipped(List<string> skipped)
        {
            skipped.Clear();

            if (HasContinuation)
            {
                skipped.AddRange(Skipped.Select(s => s.WfmId).ToArray());
            }
        }

        public ResultModel AsResult()
        {
            return new ResultModel
            {
                CreatedCount = Created.Count,
                UpdatedCount = Updated.Count,
                DeletedCount = Deleted.Count,
                FailedCount = Failed.Count,
                SkippedCount = Skipped.Count
            };
        }

        public void FailedChange(T model)
        {
            RemoveChange(model);
            _failed.TryAdd(model, model);
        }

        public void RemoveSkipped(IEnumerable<string> ids)
        {
            var skipped = All.Where(s => ids.Contains(s.WfmId)).ToArray();

            foreach (var shift in skipped)
            {
                SkippedChange(shift);
            }
        }

        public void SkippedChange(T model)
        {
            RemoveChange(model);
            _skipped.TryAdd(model, model);
        }

        private void RemoveChange(T model)
        {
            _created.TryRemove(model, out var created);
            _updated.TryRemove(model, out var updated);
            _deleted.TryRemove(model, out var deleted);
        }
    }
}
