// ---------------------------------------------------------------------------
// <copyright file="BlueYonderAvailabilityMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Mappings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Helpers;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Models;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderAvailabilityMap : IAvailabilityMap
    {
        private readonly BlueYonderPersonaOptions _options;

        private readonly ISystemTimeService _timeService;

        public BlueYonderAvailabilityMap(BlueYonderPersonaOptions options, ISystemTimeService timeService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        /// <summary>
        /// Converts availability from Teams to availability for Blue Yonder
        /// </summary>
        /// <param name="existingAvailability">The existing availability from Blue Yonder</param>
        /// <param name="availabilityModel">
        /// The availability from Teams to update the existing availability with
        /// </param>
        /// <returns>The existing availability updated with the availability from Teams</returns>
        public EmployeeAvailabilityResource MapAvailability(EmployeeAvailabilityCollectionResource existingAvailability, EmployeeAvailabilityModel availabilityModel)
        {
            // select the availability record containing the current date
            var localNow = _timeService.UtcNow.ApplyTimeZoneOffset(availabilityModel.TimeZoneInfoId);
            // in Blue Yonder, EffectiveFrom and EndsAfter are dates with 0 times, so we should
            // convert localNow to the same to ensure the comparisons work correctly
            localNow = new DateTime(localNow.Year, localNow.Month, localNow.Day);

            var currentAvailability = existingAvailability.Entities?.FirstOrDefault(e => localNow >= e.EffectiveFrom && (!e.EndsAfter.HasValue || localNow <= e.EndsAfter.Value));
            var rotationalWeekNumber = 1;

            if (currentAvailability == null)
            {
                currentAvailability = new EmployeeAvailabilityResource
                {
                    EffectiveFrom = availabilityModel.StartDate.ApplyTimeZoneOffset(availabilityModel.TimeZoneInfoId),
                    EndsAfter = availabilityModel.EndDate.HasValue ? availabilityModel.EndDate.Value.ApplyTimeZoneOffset(availabilityModel.TimeZoneInfoId) : availabilityModel.EndDate,
                    GeneralAvailability = new List<GeneralAvailability>(),
                    EmployeeId = int.Parse(availabilityModel.WfmEmployeeId),
                    NumberOfWeeks = 1
                };
                currentAvailability.CycleBaseDate = currentAvailability.EffectiveFrom;
            }
            else
            {
                // Blue Yonder allows for availability to be different for different weeks on an
                // alternating basis, however, Teams only allows a single 'current' availability
                // pattern to be defined per user This means that we need to identify the item in
                // the rotational availability that represents the current week.
                rotationalWeekNumber = (((int)((_timeService.UtcNow - currentAvailability.CycleBaseDate).TotalDays / 7)) % currentAvailability.NumberOfWeeks) + 1;

                // remove all existing general availability items having the rotational week number
                var generalAvailability = currentAvailability.GeneralAvailability.ToList();
                generalAvailability.RemoveAll(a => a.WeekNumber == rotationalWeekNumber);
                currentAvailability.GeneralAvailability = generalAvailability;
            }

            // add all the general availability items for the current rotational week
            foreach (var availabilityItem in availabilityModel.Availability)
            {
                var genAvailability = new GeneralAvailability
                {
                    DayOfWeek = availabilityItem.DayOfWeek.ToString(),
                    StartTimeOffset = DateTimeHelper.ConvertToLocalTime(availabilityItem.StartTime, availabilityModel.TimeZoneInfoId, _timeService),
                    EndTimeOffset = DateTimeHelper.ConvertToLocalTime(availabilityItem.EndTime, availabilityModel.TimeZoneInfoId, _timeService),
                    WeekNumber = rotationalWeekNumber
                };

                // for all day availability, Blue Yonder expects the StartTimeOffset = 00:00:00 and
                // EndTimeOffset = 1.00:00:00
                if (genAvailability.StartTimeOffset == "00:00:00" && genAvailability.EndTimeOffset == "00:00:00")
                {
                    genAvailability.EndTimeOffset = "1.00:00:00";
                }
                currentAvailability.GeneralAvailability.Add(genAvailability);
            }

            return currentAvailability;
        }

        /// <summary>
        /// Converts availability from Blue Yonder into the common availability understood by the
        /// core connector.
        /// </summary>
        /// <param name="availabilityCollection">The availability collection from Blue Yonder</param>
        /// <param name="timeZoneInfoId">The time zone to use to convert the times to UTC</param>
        /// <returns>The Blue Yonder availability in core connector format.</returns>
        public EmployeeAvailabilityModel MapAvailability(EmployeeAvailabilityCollectionResource availabilityCollection, string timeZoneInfoId)
        {
            var employeeAvailability = new EmployeeAvailabilityModel();

            if (availabilityCollection.Entities.Count == 0)
            {
                return employeeAvailability;
            }

            // select the availability record containing the current date
            var localNow = _timeService.UtcNow.ApplyTimeZoneOffset(timeZoneInfoId);
            // in Blue Yonder, EffectiveFrom and EndsAfter are dates with 0 times, so we should
            // convert localNow to the same to ensure the comparisons work correctly
            localNow = new DateTime(localNow.Year, localNow.Month, localNow.Day);

            var currentAvailability = availabilityCollection.Entities.FirstOrDefault(e => localNow >= e.EffectiveFrom && (!e.EndsAfter.HasValue || localNow <= e.EndsAfter.Value));

            if (currentAvailability == null)
            {
                // as there is no current availability for the employee, there is nothing to sync,
                // so just return the empty availability
                return employeeAvailability;
            }

            employeeAvailability.StartDate = currentAvailability.EffectiveFrom.ConvertFromLocalTime(timeZoneInfoId, _timeService);
            employeeAvailability.EndDate = currentAvailability.EndsAfter.HasValue ? currentAvailability.EndsAfter.Value.ConvertFromLocalTime(timeZoneInfoId, _timeService) : currentAvailability.EndsAfter;
            employeeAvailability.NumberOfWeeks = currentAvailability.NumberOfWeeks;
            employeeAvailability.WfmEmployeeId = currentAvailability.EmployeeId.ToString();
            employeeAvailability.CycleBaseDate = currentAvailability.CycleBaseDate;
            employeeAvailability.TimeZoneInfoId = timeZoneInfoId;

            // as Teams can only handle the current rotational week, we may as well filter out all
            // the other rotational weeks
            var rotationalWeekNumber = (((int)((_timeService.UtcNow - currentAvailability.CycleBaseDate).TotalDays / 7)) % currentAvailability.NumberOfWeeks) + 1;

            foreach (var availability in currentAvailability.GeneralAvailability)
            {
                if (availability.WeekNumber == rotationalWeekNumber)
                {
                    var availabilityModel = new AvailabilityModel
                    {
                        DayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), availability.DayOfWeek, true),
                        WeekNumber = availability.WeekNumber
                    };

                    if (availability.StartTimeOffset == "00:00:00" && availability.EndTimeOffset == "1.00:00:00")
                    {
                        // this is how Blue Yonder represents all day so just convert 00:00:00 -
                        // 00:00:00 local to UTC
                        availabilityModel.StartTime = DateTimeHelper.ConvertFromLocalTime("00:00:00", timeZoneInfoId, _timeService);
                        availabilityModel.EndTime = DateTimeHelper.ConvertFromLocalTime("00:00:00", timeZoneInfoId, _timeService);
                        employeeAvailability.Availability.Add(availabilityModel);
                    }
                    else if (availability.EndTimeOffset.StartsWith("1."))
                    {
                        // this is how Blue Yonder represents an end time that extends into the next
                        // day, so we should normalise this into separate days unless the part for
                        // the next day is already an all day availability in which case the
                        // overlapping piece would be redundant (we remove these in the final step)
                        availabilityModel.StartTime = DateTimeHelper.ConvertFromLocalTime(availability.StartTimeOffset, timeZoneInfoId, _timeService);
                        availabilityModel.EndTime = DateTimeHelper.ConvertFromLocalTime("00:00:00", timeZoneInfoId, _timeService);
                        employeeAvailability.Availability.Add(availabilityModel);

                        var nextDay = ((int)availabilityModel.DayOfWeek) + 1;
                        if (nextDay > 6)
                        {
                            nextDay = 0;
                        }
                        var nextDayOfWeek = (DayOfWeek)nextDay;

                        var endTime = availability.EndTimeOffset.Substring(2);
                        var newAvailabilityModel = new AvailabilityModel
                        {
                            DayOfWeek = nextDayOfWeek,
                            WeekNumber = availabilityModel.WeekNumber,
                            StartTime = DateTimeHelper.ConvertFromLocalTime("00:00:00", timeZoneInfoId, _timeService),
                            EndTime = DateTimeHelper.ConvertFromLocalTime(endTime, timeZoneInfoId, _timeService)
                        };
                        employeeAvailability.Availability.Add(newAvailabilityModel);
                    }
                    else
                    {
                        availabilityModel.StartTime = DateTimeHelper.ConvertFromLocalTime(availability.StartTimeOffset, timeZoneInfoId, _timeService);
                        availabilityModel.EndTime = DateTimeHelper.ConvertFromLocalTime(availability.EndTimeOffset, timeZoneInfoId, _timeService);
                        employeeAvailability.Availability.Add(availabilityModel);
                    }
                }
            }

            return RemoveRedundantEntries(employeeAvailability, timeZoneInfoId);
        }

        private EmployeeAvailabilityModel RemoveRedundantEntries(EmployeeAvailabilityModel employeeAvailability, string timeZoneInfoId)
        {
            var availabilityToRemove = new List<AvailabilityModel>();
            foreach (var dayOfWeek in employeeAvailability.Availability.Where(a => DateTimeHelper.ConvertToLocalTime(a.StartTime, timeZoneInfoId, _timeService) == "00:00:00" && DateTimeHelper.ConvertToLocalTime(a.EndTime, timeZoneInfoId, _timeService) == "00:00:00").Select(a => a.DayOfWeek))
            {
                // for each of these days, we should only have a single record, if we have more than
                // one then we have a redundant record which should be removed
                if (employeeAvailability.Availability.Count(a => a.DayOfWeek == dayOfWeek) > 1)
                {
                    availabilityToRemove.AddRange(employeeAvailability.Availability.Where(a => a.DayOfWeek == dayOfWeek && (DateTimeHelper.ConvertToLocalTime(a.StartTime, timeZoneInfoId, _timeService) != "00:00:00" || DateTimeHelper.ConvertToLocalTime(a.EndTime, timeZoneInfoId, _timeService) != "00:00:00")));
                }
            }

            foreach (var availability in availabilityToRemove)
            {
                employeeAvailability.Availability.Remove(availability);
            }

            return employeeAvailability;
        }
    }
}
