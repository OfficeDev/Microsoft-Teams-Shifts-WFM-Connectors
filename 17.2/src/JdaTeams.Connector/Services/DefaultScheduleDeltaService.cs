using JdaTeams.Connector.Models;
using System.Collections.Generic;
using System.Linq;

namespace JdaTeams.Connector.Services
{
    public class DefaultScheduleDeltaService : IScheduleDeltaService
    {
        public DeltaModel ComputeDelta(IEnumerable<ShiftModel> from, IEnumerable<ShiftModel> to)
        {
            var fromLookup = from?.ToDictionary(s => s.JdaShiftId)
                ?? new Dictionary<string, ShiftModel>();
            var toLookup = to?.ToDictionary(s => s.JdaShiftId)
                ?? new Dictionary<string, ShiftModel>();

            var createdKeys = toLookup.Keys.Except(fromLookup.Keys);
            var createdShifts = createdKeys.Select(k => toLookup[k]);

            var existingKeys = toLookup.Keys.Intersect(fromLookup.Keys);
            var existingShifts = existingKeys.Select(k => UpdateIdFields(fromLookup[k], toLookup[k])).ToArray();

            var updatedShifts = existingKeys
                .Where(k => HasChanges(fromLookup[k], toLookup[k]))
                .Select(k => toLookup[k]);

            var deletedShifts = fromLookup.Keys.Except(toLookup.Keys)
                .Select(k => fromLookup[k]);

            return new DeltaModel(createdShifts, updatedShifts, deletedShifts);
        }

        public bool HasChanges(ShiftModel from, ShiftModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.JdaEmployeeId != to.JdaEmployeeId
                || from.JdaJobId != to.JdaJobId
                || from.Jobs?.Count != to.Jobs?.Count
                || from.Jobs?.Any(a => HasJobChanges(a, to.Jobs[from.Jobs.IndexOf(a)])) == true
                || from.Activities?.Count != to.Activities?.Count
                || from.Activities?.Any(a => HasActivityChanges(a, to.Activities[from.Activities.IndexOf(a)])) == true;
        }

        public bool HasJobChanges(ActivityModel from, ActivityModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.JdaJobId != to.JdaJobId;
        }

        public bool HasActivityChanges(ActivityModel from, ActivityModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.Code != to.Code;
        }

        private ShiftModel UpdateIdFields(ShiftModel from, ShiftModel to)
        {
            to.TeamsShiftId = from.TeamsShiftId;
            to.TeamsEmployeeId = (to.JdaEmployeeId == from.JdaEmployeeId ? from.TeamsEmployeeId : null);
            to.TeamsSchedulingGroupId = (to.JdaJobId == from.JdaJobId ? from.TeamsSchedulingGroupId : null);
            return to;
        }
    }
}
