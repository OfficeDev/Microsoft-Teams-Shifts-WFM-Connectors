using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Models;
using JdaTeams.Connector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph.Mappings
{
    public class MicrosoftGraphShiftMap : IShiftMap
    {
        private readonly IShiftThemeMap _shiftThemeMap;

        public MicrosoftGraphShiftMap(IShiftThemeMap shiftThemeMap)
        {
            _shiftThemeMap = shiftThemeMap ?? throw new ArgumentNullException(nameof(shiftThemeMap));
        }

        public ShiftItem MapShift(ShiftModel shift)
        {
            var shiftItem = new ShiftItem
            {
                StartDateTime = shift.StartDate,
                EndDateTime = shift.EndDate,
                Activities = MapActivities(shift),
                Theme = _shiftThemeMap.MapTheme(shift.ThemeCode)
            };

            return shiftItem;
        }

        private List<ShiftActivity> MapActivities(ShiftModel shift)
        {
            var mappedActivities = new List<ShiftActivity>();
            // we are relying on the fact that jobs and activities that start at the same
            // time will be ordered Job followed by Activity and it is important that this is the
            // case for the overlapping logic below
            var activities = shift.Jobs.Union(shift.Activities);

            foreach (var activity in activities.OrderBy(a => a.StartDate))
            {
                if (activity.Code?.Equals("break", StringComparison.OrdinalIgnoreCase) == false)
                {
                    var shiftActivity = new ShiftActivity
                    {
                        DisplayName = activity.Code,
                        IsPaid = !activity.Code.Equals("meal", StringComparison.OrdinalIgnoreCase),
                        StartDateTime = activity.StartDate,
                        EndDateTime = activity.EndDate,
                        Theme = _shiftThemeMap.MapTheme(activity.ThemeCode)
                    };
                    mappedActivities.Add(shiftActivity);

                    if (mappedActivities.Count > 1)
                    {
                        var prevActivity = mappedActivities[mappedActivities.Count - 2];
                        var prevActivityEndDateTime = prevActivity.EndDateTime.Value;
                        if (shiftActivity.StartDateTime.Value < prevActivity.EndDateTime.Value)
                        {
                            // this activity is contained within the previous activity so handle 
                            // the previous activity appropriately
                            if (prevActivity.StartDateTime == shiftActivity.StartDateTime)
                            {
                                // as the previous actity starts at the same time as the current
                                // activity, we should remove the previous activity (we will be
                                // adding the part of the previous activity that comes after this
                                // activity below)
                                mappedActivities.Remove(prevActivity);
                            }
                            else
                            {
                                // we should split the previous activity into an activity
                                // before this activity and one after, so terminate the previous
                                // activity when this activity starts
                                prevActivity.EndDateTime = shiftActivity.StartDateTime;
                            }
                        }

                        if (shiftActivity.EndDateTime.Value < prevActivityEndDateTime)
                        {
                            // this activity is contained entirely within the previous activity
                            // so we have to split the previous activity into an activity before 
                            // (prevActivity) and a new one after
                            shiftActivity = new ShiftActivity
                            {
                                DisplayName = prevActivity.DisplayName,
                                IsPaid = prevActivity.IsPaid,
                                StartDateTime = shiftActivity.EndDateTime,
                                EndDateTime = prevActivityEndDateTime,
                                Theme = prevActivity.Theme
                            };
                            mappedActivities.Add(shiftActivity);
                        }
                    }
                }
            }

            return mappedActivities;
        }

    }
}
