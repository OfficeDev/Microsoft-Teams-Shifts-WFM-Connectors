// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request Item for Time off.
    /// </summary>
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets time off request item details.
        /// </summary>
        [XmlElement("GlobalTimeOffRequestItem")]
        public GlobalTimeOffRequestItem GlobalTimeOffRequestItem { get; set; }
    }
}
