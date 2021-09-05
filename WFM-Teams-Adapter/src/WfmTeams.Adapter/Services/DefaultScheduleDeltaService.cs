// ---------------------------------------------------------------------------
// <copyright file="DefaultScheduleDeltaService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using WfmTeams.Adapter.Models;

    public class DefaultScheduleDeltaService : DefaultDeltaService<ShiftModel>
    {
        protected override Dictionary<string, ShiftModel> GetLookup(IEnumerable<ShiftModel> list)
        {
            // although there should not be duplicate items in the list, we have observed in
            // production that there is some scenario where this can in fact happen and if so we do
            // not want the sync to fail, so must handle it here
            // TODO: we can revert to the original way of creating the dictionary once the underlying issue
            // with duplicate shifts is found and fixed
            var dictionary = new Dictionary<string, ShiftModel>();
            if (list != null)
            {
                foreach (var shift in list)
                {
                    dictionary[shift.WfmShiftId] = shift;
                }
            }

            return dictionary;
        }

        protected override bool HasChanges(ShiftModel from, ShiftModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.WfmEmployeeId != to.WfmEmployeeId
                || from.WfmJobId != to.WfmJobId
                || from.Quantity != to.Quantity
                || from.Jobs?.Count != to.Jobs?.Count
                || from.Jobs?.Any(a => HasJobChanges(a, to.Jobs[from.Jobs.IndexOf(a)])) == true
                || from.Activities?.Count != to.Activities?.Count
                || from.Activities?.Any(a => HasActivityChanges(a, to.Activities[from.Activities.IndexOf(a)])) == true;
        }

        protected override ShiftModel UpdateIdFields(ShiftModel from, ShiftModel to)
        {
            to.TeamsShiftId = from.TeamsShiftId;
            to.TeamsEmployeeId = (to.WfmEmployeeId == from.WfmEmployeeId ? from.TeamsEmployeeId : null);
            to.TeamsSchedulingGroupId = (to.WfmJobId == from.WfmJobId ? from.TeamsSchedulingGroupId : null);

            return to;
        }

        private bool HasActivityChanges(ActivityModel from, ActivityModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.Code != to.Code;
        }

        private bool HasJobChanges(ActivityModel from, ActivityModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.WfmJobId != to.WfmJobId;
        }
    }
}
