// <copyright file="SchedulePayCodeEdit.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the SchedulePayCodeEdit.
    /// </summary>
    public class SchedulePayCodeEdit
    {
        /// <summary>
        /// Gets or sets the employee.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PersonIdentity> Employee { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets a value indicating whether there is a locked flag.
        /// </summary>
        [XmlAttribute]
        public bool LockedFlag { get; set; }

        /// <summary>
        /// Gets or sets the startDate.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the paycode has been deleted.
        /// </summary>
        [XmlAttribute]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the amountInTime.
        /// </summary>
        [XmlAttribute]
        public string AmountInTime { get; set; }

        /// <summary>
        /// Gets or sets the displayTime.
        /// </summary>
        [XmlAttribute]
        public string DisplayTime { get; set; }

        /// <summary>
        /// Gets or sets the orgJobPath.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the payCodeName.
        /// </summary>
        [XmlAttribute]
        public string PayCodeName { get; set; }
    }
}