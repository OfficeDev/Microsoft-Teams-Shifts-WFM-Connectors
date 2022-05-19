// <copyright file="AccrualData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Accrual
{
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using System.Xml.Serialization;

    /// <summary>
    /// The AccrualData tag.
    /// </summary>
    public class AccrualData
    {
        /// <summary>
        /// Gets or sets the BalanceDate.
        /// </summary>
        [XmlAttribute(AttributeName = "BalanceDate")]
        public string BalanceDate { get; set; }

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement(ElementName = "Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the AccrualBalances.
        /// </summary>
        [XmlElement(ElementName = "AccrualBalances")]
        public AccrualBalances AccrualBalances { get; set; }
    }
}