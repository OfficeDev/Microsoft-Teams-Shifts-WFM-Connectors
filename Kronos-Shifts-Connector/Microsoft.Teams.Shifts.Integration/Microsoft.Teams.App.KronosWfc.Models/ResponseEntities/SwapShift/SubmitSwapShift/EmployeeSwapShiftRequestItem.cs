// <copyright file="EmployeeSwapShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the EmployeeSwapShiftRequestItem.
    /// </summary>
    public class EmployeeSwapShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlAttribute]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the DateTime.
        /// </summary>
        [XmlAttribute]
        public string DateTime { get; set; }

        /// <summary>
        /// Gets or sets the OfferedShift.
        /// </summary>
        [XmlElement]
        public OfferedShift OfferedShift { get; set; }

        /// <summary>
        /// Gets or sets the RequestedShift.
        /// </summary>
        [XmlElement]
        public RequestedShift RequestedShift { get; set; }
    }
}