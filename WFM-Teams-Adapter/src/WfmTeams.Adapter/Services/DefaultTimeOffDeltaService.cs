// ---------------------------------------------------------------------------
// <copyright file="DefaultTimeOffDeltaService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using WfmTeams.Adapter.Models;

    public class DefaultTimeOffDeltaService : DefaultDeltaService<TimeOffModel>
    {
        protected override Dictionary<string, TimeOffModel> GetLookup(IEnumerable<TimeOffModel> list)
        {
            return list?.ToDictionary(s => s.WfmTimeOffId) ?? new Dictionary<string, TimeOffModel>();
        }

        protected override bool HasChanges(TimeOffModel from, TimeOffModel to)
        {
            return from.StartDate != to.StartDate
                || from.EndDate != to.EndDate
                || from.WfmEmployeeId != to.WfmEmployeeId
                || from.WfmTimeOffTypeId != to.WfmTimeOffTypeId
                || from.WfmTimeOffReason != to.WfmTimeOffReason;
        }

        protected override TimeOffModel UpdateIdFields(TimeOffModel from, TimeOffModel to)
        {
            to.TeamsTimeOffId = from.TeamsTimeOffId;
            to.TeamsEmployeeId = (to.WfmEmployeeId == from.WfmEmployeeId ? from.TeamsEmployeeId : null);
            to.TeamsTimeOffReasonId = (to.WfmTimeOffTypeId == from.WfmTimeOffTypeId ? from.TeamsTimeOffReasonId : null);

            return to;
        }
    }
}
