// <copyright file="RequestFor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Requests For details.
    /// </summary>
    public class RequestFor
    {
        /// <summary>
        /// Gets or sets for which it is requested for.
        /// </summary>
        [XmlText]
        public string For { get; set; }
    }
}
