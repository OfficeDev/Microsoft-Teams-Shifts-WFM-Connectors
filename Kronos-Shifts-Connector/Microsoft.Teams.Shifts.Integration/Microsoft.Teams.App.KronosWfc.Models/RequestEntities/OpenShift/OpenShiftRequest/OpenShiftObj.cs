﻿// <copyright file="OpenShiftObj.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
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
        public Common.ShiftSegments OpenShiftSegments { get; set; }
    }
}