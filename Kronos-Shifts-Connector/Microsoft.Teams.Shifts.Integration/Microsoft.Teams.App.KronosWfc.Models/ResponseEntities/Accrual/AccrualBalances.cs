// <copyright file="AccrualBalances.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Accrual
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// The AccrualBalances tag.
    /// </summary>
    public class AccrualBalances
    {
        /// <summary>
        /// Gets or sets the Accrual Balance Summary.
        /// </summary>
        [XmlElement(ElementName = "AccrualBalanceSummary")]
        public List<AccrualBalanceSummary> AccrualBalanceSummaries { get; set; }
    }
}