// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [Serializable]
    public class Request
    {
        /// <summary>
        /// Gets or sets the Schedule.
        /// </summary>
        public ScheduleReq Schedule { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}