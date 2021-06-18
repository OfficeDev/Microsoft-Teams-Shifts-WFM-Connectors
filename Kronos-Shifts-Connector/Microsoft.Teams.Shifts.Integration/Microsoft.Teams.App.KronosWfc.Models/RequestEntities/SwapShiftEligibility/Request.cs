// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShiftEligibility
{
    using System.Xml.Serialization;

    /// <summary>
    /// The request to get the employees eligible for a shift swap.
    /// </summary>
    public class Request : Common.Request
    {
        /// <summary>
        /// Gets or Sets the swap shift employees tag.
        /// </summary>
        [XmlElement]
        public SwapShiftEmployeesTag SwapShiftEmployees { get; set; }
    }
}
