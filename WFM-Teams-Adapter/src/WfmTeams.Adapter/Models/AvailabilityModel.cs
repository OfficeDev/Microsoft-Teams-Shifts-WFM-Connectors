// ---------------------------------------------------------------------------
// <copyright file="AvailabilityModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    /// <summary>
    /// Defines the model of an employee's availability to work.
    /// </summary>
    public class AvailabilityModel
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string EndTime { get; set; }
        public string StartTime { get; set; }

        /// <summary>
        /// WFM providers may support rotational availability which means that it is possible for an
        /// employee to specify different availability for different weeks on a rotational basis.
        /// </summary>
        /// <remarks>
        /// A 2 week Rotational availability means that an employee can specify that in: Week 1 -
        /// they are available Mon-Fri 9-5 Week 2 - they are available Mon-Sun 8-4 Week 3 will be
        /// the same as week 1 Week 4 will be the same as week 2 Week 5 will be the same as week 1 etc.
        /// </remarks>
        public int WeekNumber { get; set; }
    }
}
