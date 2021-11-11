// ---------------------------------------------------------------------------
// <copyright file="DefaultAvailabilityDeltaService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using WfmTeams.Adapter.Models;

    public class DefaultAvailabilityDeltaService : DefaultDeltaService<EmployeeAvailabilityModel>
    {
        protected override Dictionary<string, EmployeeAvailabilityModel> GetLookup(IEnumerable<EmployeeAvailabilityModel> list)
        {
            return list?.ToDictionary(s => s.TeamsEmployeeId) ?? new Dictionary<string, EmployeeAvailabilityModel>();
        }

        protected override bool HasChanges(EmployeeAvailabilityModel from, EmployeeAvailabilityModel to)
        {
            if ((from.Availability == null) && (to.Availability == null))
            {
                return false;
            }
            else if ((from.Availability == null) || (to.Availability == null))
            {
                return true;
            }

            var compareFrom = from.Availability.OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime).ToList();
            var compareTo = to.Availability.OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime).ToList();

            if (compareFrom.Count != compareTo.Count)
            {
                return true;
            }

            for (int i = 0; i < compareFrom.Count; i++)
            {
                if (HasAvailabilityChanges(compareFrom[i], compareTo[i]))
                {
                    return true;
                }
            }

            return false;
        }

        protected override EmployeeAvailabilityModel UpdateIdFields(EmployeeAvailabilityModel from, EmployeeAvailabilityModel to)
        {
            // nothing to do
            return to;
        }

        private bool HasAvailabilityChanges(AvailabilityModel from, AvailabilityModel to)
        {
            if (to == null)
            {
                return true;
            }

            return from.StartTime != to.StartTime
                || from.EndTime != to.EndTime
                || from.DayOfWeek != to.DayOfWeek
                || from.WeekNumber != to.WeekNumber;
        }
    }
}
