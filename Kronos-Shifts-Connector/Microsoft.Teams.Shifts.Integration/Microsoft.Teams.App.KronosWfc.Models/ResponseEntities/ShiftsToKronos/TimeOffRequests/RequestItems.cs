// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request Items.
    /// </summary>
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets the time off request items.
        /// </summary>
        [XmlElement("EmployeeGlobalTimeOffRequestItem")]
#pragma warning disable CA1819 // Properties should not return arrays
        public EmployeeGlobalTimeOffRequestItem[] GlobalTimeOffRequestItms { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
