// <copyright file="ShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift
{
    using System.Xml.Serialization;
    using static Microsoft.Teams.App.KronosWfc.Models.RequestEntities.RequestManagementSwap;

    /// <summary>
    /// This class models the ShiftRequestItem.
    /// </summary>
    public class ShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the StartDateTime.
        /// </summary>
        [XmlAttribute]
        public string StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDateTime.
        /// </summary>
        [XmlAttribute]
        public string EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement]
        public Employee Employee { get; set; }
    }
}