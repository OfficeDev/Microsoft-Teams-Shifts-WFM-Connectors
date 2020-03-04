// <copyright file="StartDate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Start Date.
    /// </summary>
    public class StartDate
    {
        /// <summary>
        /// Gets or sets date.
        /// </summary>
        [XmlText]
        public string Date { get; set; }
    }
}
