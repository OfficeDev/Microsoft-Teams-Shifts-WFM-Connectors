// <copyright file="ShiftSegment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Schedule Segments.
    /// </summary>
    public class ShiftSegment
    {
        /// <summary>
        /// Gets or sets the StartDayNumber.
        /// </summary>
        [XmlAttribute]
        public int StartDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the EndDayNumber.
        /// </summary>
        [XmlAttribute]
        public int EndDayNumber { get; set; }

        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        [XmlAttribute]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the EndTime.
        /// </summary>
        [XmlAttribute]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Creates a Shift Segment.
        /// </summary>
        /// <param name="startTime">The start time of the shift segment.</param>
        /// <param name="endTime">The end time of the shift segment.</param>
        /// <param name="startDayNumber">The start day number of the shift segment.</param>
        /// <param name="endDayNumber">The end day number of the shift segment.</param>
        /// <param name="jobPath">The job path of the shift segment.</param>
        /// <returns>A <see cref="ShiftSegment"/>.</returns>
        public ShiftSegment Create(
            string startTime,
            string endTime,
            int startDayNumber,
            int endDayNumber,
            string jobPath)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.StartDayNumber = startDayNumber;
            this.EndDayNumber = endDayNumber;
            this.OrgJobPath = jobPath;

            return this;
        }

        /// <summary>
        /// Creates a Shift Segment.
        /// </summary>
        /// <param name="startTime">The start time of the shift segment.</param>
        /// <param name="endTime">The end time of the shift segment.</param>
        /// <param name="startDayNumber">The start day number of the shift segment.</param>
        /// <param name="endDayNumber">The end day number of the shift segment.</param>
        /// <param name="jobPath">The job path of the shift segment.</param>
        /// <returns>A <see cref="ShiftSegment"/>.</returns>
        public List<ShiftSegment> CreateList(
            string startTime,
            string endTime,
            int startDayNumber,
            int endDayNumber,
            string jobPath)
        {
            return new List<ShiftSegment>
            {
                new ShiftSegment
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    StartDayNumber = startDayNumber,
                    EndDayNumber = endDayNumber,
                    OrgJobPath = jobPath,
                },
            };
        }
    }
}