// <copyright file="BaseWageRate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the BaseWageRate.
    /// </summary>
    public class BaseWageRate
    {
        /// <summary>
        /// Gets or sets the HourlyRate.
        /// </summary>
        [XmlAttribute]
        public string HourlyRate { get; set; }

        /// <summary>
        /// Gets or sets the EffectiveDate.
        /// </summary>
        [XmlAttribute]
        public string EffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the ExpirationDate.
        /// </summary>
        [XmlAttribute]
        public string ExpirationDate { get; set; }
    }
}