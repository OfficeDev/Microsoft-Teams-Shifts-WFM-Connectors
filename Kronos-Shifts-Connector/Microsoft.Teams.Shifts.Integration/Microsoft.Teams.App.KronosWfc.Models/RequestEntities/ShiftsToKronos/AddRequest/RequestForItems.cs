// <copyright file="RequestForItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Requests For Item details.
    /// </summary>
    public class RequestForItems
    {
        /// <summary>
        /// Gets or sets requested item details.
        /// </summary>
        [XmlElement("RequestFor")]
        public RequestFor RequestFor { get; set; }
    }
}
