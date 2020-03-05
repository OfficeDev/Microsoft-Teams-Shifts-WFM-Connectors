// <copyright file="BaseWageRates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// Gets or sets the BaseWageRates.
    /// </summary>
    public class BaseWageRates
    {
        /// <summary>
        /// Gets or sets the array of BaseWageRates.
        /// </summary>
        [XmlElement("BaseWageRate")]
#pragma warning disable CA1819 // Properties should not return arrays
        public BaseWageRate[] BaseWageRt { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}