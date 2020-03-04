// <copyright file="RequestedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestedShift.
    /// </summary>
    public class RequestedShift
    {
        /// <summary>
        /// Gets or sets the ShiftRequestItem.
        /// </summary>
        [XmlElement]
        public ShiftRequestItem ShiftRequestItem { get; set; }
    }
}