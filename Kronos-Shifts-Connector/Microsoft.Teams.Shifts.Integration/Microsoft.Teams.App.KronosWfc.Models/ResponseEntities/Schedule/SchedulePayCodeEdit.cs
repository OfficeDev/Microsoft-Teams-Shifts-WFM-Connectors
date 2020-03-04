// <copyright file="SchedulePayCodeEdit.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Schedule
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the SchedulePayCodeEdit.
    /// </summary>
    public class SchedulePayCodeEdit
    {
        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PersonIdentity> Employee { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets a value indicating whether something is locked.
        /// </summary>
        [XmlAttribute]
        public bool LockedFlag { get; set; }

        /// <summary>
        /// Gets or sets the StartDate.
        /// </summary>
        [XmlAttribute]
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not a record is deleted.
        /// </summary>
        [XmlAttribute]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the AmountInTime.
        /// </summary>
        [XmlAttribute]
        public string AmountInTime { get; set; }

        /// <summary>
        /// Gets or sets the DisplayTime.
        /// </summary>
        [XmlAttribute]
        public string DisplayTime { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the PayCodeName.
        /// </summary>
        [XmlAttribute]
        public string PayCodeName { get; set; }
    }
}