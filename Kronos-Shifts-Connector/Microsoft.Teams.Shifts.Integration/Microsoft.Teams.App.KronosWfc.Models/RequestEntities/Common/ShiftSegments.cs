// <copyright file="ShiftSegments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Schedule Segments.
    /// </summary>
    public class ShiftSegments
    {
        
        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlElement]
        public List<ShiftSegment> ShiftSegment { get; set; }

        /// <summary>
        /// Creates a Shift Segments object.
        /// </summary>
        /// <param name="startTime">The start time of the shift segment.</param>
        /// <param name="endTime">The end time of the shift segment.</param>
        /// <param name="startDayNumber">The start day number of the shift segment.</param>
        /// <param name="endDayNumber">The end day number of the shift segment.</param>
        /// <param name="jobPath">The job path of the shift segment.</param>
        /// <returns>A <see cref="ShiftSegment"/>.</returns>
        public ShiftSegments Create(
            string startTime,
            string endTime,
            int startDayNumber,
            int endDayNumber,
            string jobPath)
        {
            this.ShiftSegment = new List<ShiftSegment>();
            this.ShiftSegment.Add(
                new ShiftSegment
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    StartDayNumber = startDayNumber,
                    EndDayNumber = endDayNumber,
                    OrgJobPath = jobPath,
                });

            return this;
        }
    }
}