// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphAvailabilityMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Mappings
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using TimeZoneConverter;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Helpers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Options;
    using WfmTeams.Adapter.Services;

    public class MicrosoftGraphAvailabilityMap : IAvailabilityMap
    {
        private readonly ConnectorOptions _options;

        private readonly ISystemTimeService _timeService;

        public MicrosoftGraphAvailabilityMap(ConnectorOptions options, ISystemTimeService timeService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        /// <summary>
        /// Maps the internal employee availability model back to Teams availability items.
        /// </summary>
        /// <param name="availabilityModel">The model to map.</param>
        /// <param name="timeZoneInfoId">The time zone id to use to convert utc back to local.</param>
        /// <returns>The list of Teams availability items.</returns>
        public IList<AvailabilityItem> MapAvailability(EmployeeAvailabilityModel availabilityModel)
        {
            // when writing availability to Teams, we need to use IANA time zones rather than
            // Windows, so convert if necessary
            if (!TZConvert.TryWindowsToIana(availabilityModel.TimeZoneInfoId, out string ianaTimeZone))
            {
                ianaTimeZone = availabilityModel.TimeZoneInfoId;
            }

            // Some WFM providers allow for availability to be different for different weeks on an
            // alternating basis, however, Teams only allows a single 'current' availability pattern
            // to be defined per user this means that we need to identify the item in the rotational
            // availability that represents the current week.
            var rotationalWeekNumber = (((int)((_timeService.UtcNow - availabilityModel.CycleBaseDate).TotalDays / 7)) % availabilityModel.NumberOfWeeks) + 1;

            var groupedByDayItems = availabilityModel.Availability
                .Where(a => a.WeekNumber == rotationalWeekNumber)
                .GroupBy(a => a.DayOfWeek.ToString(), a => new TimeSlotItem { StartTime = a.StartTime, EndTime = a.EndTime }, (key, g) =>
                    new
                    {
                        Day = key,
                        TimeSlots = g.ToList()
                    });

            var availabilityItems = new List<AvailabilityItem>();
            foreach (var item in groupedByDayItems)
            {
                var availabilityItem = new AvailabilityItem
                {
                    Recurrence = new RecurrenceItem
                    {
                        Pattern = new PatternItem
                        {
                            DaysOfWeek = new List<string> { item.Day },
                            Interval = 1,
                            Type = "Weekly"
                        },
                        Range = new RangeItem
                        {
                            Type = "noEnd"
                        }
                    },
                    TimeSlots = new List<TimeSlotItem>(),
                    TimeZone = ianaTimeZone
                };

                foreach (var timeSlot in item.TimeSlots)
                {
                    availabilityItem.TimeSlots.Add(new TimeSlotItem
                    {
                        StartTime = DateTimeHelper.ConvertToLocalTime(timeSlot.StartTime, availabilityModel.TimeZoneInfoId, _timeService),
                        EndTime = DateTimeHelper.ConvertToLocalTime(timeSlot.EndTime, availabilityModel.TimeZoneInfoId, _timeService)
                    });
                }

                availabilityItems.Add(availabilityItem);
            }

            if (availabilityItems.Count < 7)
            {
                AddUnavailableDays(availabilityItems, ianaTimeZone);
            }

            return availabilityItems;
        }

        /// <summary>
        /// Maps Teams availability items to the internal employee availability model representation.
        /// </summary>
        /// <param name="availabilityItems">The list of Teams availability items</param>
        /// <param name="userId">The Teams user ID</param>
        /// <returns>The internal model.</returns>
        public EmployeeAvailabilityModel MapAvailability(IList<AvailabilityItem> availabilityItems, string userId)
        {
            var availabilityModel = new EmployeeAvailabilityModel
            {
                TeamsEmployeeId = userId,
                Availability = new List<AvailabilityModel>(),
                StartDate = _timeService.UtcNow.StartOfWeek(_options.StartDayOfWeek),
                // in Teams shift preferences have no end date, they continue indefinitely until changed
                EndDate = null,
                // Teams only has single week availability
                NumberOfWeeks = 1,
                TimeZoneInfoId = availabilityItems.Count > 0 ? availabilityItems.First().TimeZone : null
            };

            foreach (var availabilityItem in availabilityItems)
            {
                foreach (var dayOfWeek in availabilityItem.Recurrence.Pattern.DaysOfWeek)
                {
                    if (availabilityItem.TimeSlots != null)
                    {
                        foreach (var timeslot in availabilityItem.TimeSlots)
                        {
                            availabilityModel.Availability.Add(new AvailabilityModel
                            {
                                DayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dayOfWeek)),
                                // convert times from whatever time zone Teams sends them to us to UTC
                                StartTime = DateTimeHelper.ConvertFromLocalTime(timeslot.StartTime, availabilityItem.TimeZone, _timeService),
                                EndTime = DateTimeHelper.ConvertFromLocalTime(timeslot.EndTime, availabilityItem.TimeZone, _timeService),
                                // as Teams only supports a single availability week, default it
                                WeekNumber = 1
                            });
                        }
                    }
                }
            }

            return availabilityModel;
        }

        /// <summary>
        /// Teams expects to receive a list of availability items that span a full week, therefore
        /// we need to add any missing days as unavailable days
        /// </summary>
        /// <param name="availabilityItems">
        /// The current list of availability items to add the unavailable days to.
        /// </param>
        /// <param name="ianaTimeZone">The time zone for the times.</param>
        private void AddUnavailableDays(List<AvailabilityItem> availabilityItems, string ianaTimeZone)
        {
            // get the list of days included in the list
            var includedDays = availabilityItems.Select(i => i.Recurrence.Pattern.DaysOfWeek[0]);
            var expectedDays = new List<string>
            {
                nameof(DayOfWeek.Sunday),
                nameof(DayOfWeek.Monday),
                nameof(DayOfWeek.Tuesday),
                nameof(DayOfWeek.Wednesday),
                nameof(DayOfWeek.Thursday),
                nameof(DayOfWeek.Friday),
                nameof(DayOfWeek.Saturday)
            };

            var missingDays = expectedDays.Except(includedDays);
            var missingAvailabilityItems = missingDays.Select(day =>
                new AvailabilityItem
                {
                    Recurrence = new RecurrenceItem
                    {
                        Pattern = new PatternItem
                        {
                            DaysOfWeek = new List<string> { day },
                            Interval = 1,
                            Type = "Weekly"
                        },
                        Range = new RangeItem
                        {
                            Type = "noEnd"
                        }
                    },
                    TimeSlots = new List<TimeSlotItem>(),
                    TimeZone = ianaTimeZone
                });

            availabilityItems.AddRange(missingAvailabilityItems);
        }
    }
}
