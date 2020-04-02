using JdaTeams.Connector.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace JdaTeams.Connector.Models
{
    public class DeltaModel
    {
        private readonly ConcurrentDictionary<ShiftModel, object> _created = new ConcurrentDictionary<ShiftModel, object>();
        private readonly ConcurrentDictionary<ShiftModel, object> _updated = new ConcurrentDictionary<ShiftModel, object>();
        private readonly ConcurrentDictionary<ShiftModel, object> _deleted = new ConcurrentDictionary<ShiftModel, object>();
        private readonly ConcurrentDictionary<ShiftModel, object> _failed = new ConcurrentDictionary<ShiftModel, object>();
        private readonly ConcurrentDictionary<ShiftModel, object> _skipped = new ConcurrentDictionary<ShiftModel, object>();

        public DeltaModel(IEnumerable<ShiftModel> created, IEnumerable<ShiftModel> updated, IEnumerable<ShiftModel> deleted)
        {
            _created.AddRange(created, k => k as ShiftModel);
            _updated.AddRange(updated, k => k as ShiftModel);
            _deleted.AddRange(deleted, k => k as ShiftModel);
        }

        public ICollection<ShiftModel> Created => _created.Keys;
        public ICollection<ShiftModel> Updated => _updated.Keys;
        public ICollection<ShiftModel> Deleted => _deleted.Keys;
        public ICollection<ShiftModel> Skipped => _skipped.Keys;
        public ICollection<ShiftModel> Failed => _failed.Keys;
        public List<ShiftModel> All => Created.Union(Updated).Union(Deleted).ToList();

        public void FailedChange(ShiftModel shiftModel)
        {
            RemovedChange(shiftModel);
            _failed.TryAdd(shiftModel, shiftModel);
        }

        public void SkippedChange(ShiftModel shiftModel)
        {
            RemovedChange(shiftModel);
            _skipped.TryAdd(shiftModel, shiftModel);
        }

        private void RemovedChange(ShiftModel shiftModel)
        {
            _created.TryRemove(shiftModel, out var created);
            _updated.TryRemove(shiftModel, out var updated);
            _deleted.TryRemove(shiftModel, out var deleted);
        }

        public bool HasChanges => Created.Any() || Updated.Any() || Deleted.Any();

        public bool HasContinuation { get; private set; }

        public void ApplyMaximum(int maximumDelta)
        {
            var continuation = All
                .Skip(maximumDelta)
                .ToArray();

            if (continuation.Any())
            {
                HasContinuation = true;

                foreach (var shiftModel in continuation)
                {
                    RemovedChange(shiftModel);
                }
            }
        }

        public void RemoveSkipped(IEnumerable<string> shiftIds)
        {
            var skippedShifts = All.Where(s => shiftIds.Contains(s.JdaShiftId)).ToArray();

            foreach (var shift in skippedShifts)
            {
                SkippedChange(shift);
            }
        }

        public void ApplySkipped(List<string> skipped)
        {
            skipped.Clear();

            if (HasContinuation)
            {
                skipped.AddRange(Skipped.Select(s => s.JdaShiftId).ToArray());
            }
        }

        public void ApplyChanges(List<ShiftModel> shifts)
        {
            var changedIds = Updated.Union(Deleted).Select(u => u.JdaShiftId).ToArray();

            shifts.RemoveAll(s => changedIds.Contains(s.JdaShiftId));
            shifts.AddRange(Created);
            shifts.AddRange(Updated);
        }

        public ResultModel AsResult()
        {
            return new ResultModel(finished: !HasContinuation)
            {
                CreatedCount = Created.Count,
                UpdatedCount = Updated.Count,
                DeletedCount = Deleted.Count,
                FailedCount = Failed.Count,
                SkippedCount = Skipped.Count
            };
        }
    }
}
