// <copyright file="OpenShiftObj.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
    using System.Collections.Generic;

    /// <summary>
    /// This class models the OpenShiftObj.
    /// </summary>
    public class OpenShiftObj
    {
        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the EndTime.
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the EndDayNumber.
        /// </summary>
        public string EndDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the StartDayNumber.
        /// </summary>
        public string StartDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the SegmentTypeName.
        /// </summary>
        public string SegmentTypeName { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        public string PersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the ShiftDate.
        /// </summary>
        public string ShiftDate { get; set; }

        /// <summary>
        /// Gets or sets the open shift segments from the activities.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment> OpenShiftSegments { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}