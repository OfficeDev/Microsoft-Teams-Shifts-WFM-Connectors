// <copyright file="AccrualBalanceSummary.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Accrual
{
    using System.Xml.Serialization;

    /// <summary>
    /// The Accrual Balances Summary tag.
    /// </summary>
    public class AccrualBalanceSummary
    {
        /// <summary>
        /// Gets or sets the AccrualCodeId.
        /// </summary>
        [XmlAttribute(AttributeName = "AccrualCodeId")]
        public string AccrualCodeId { get; set; }

        /// <summary>
        /// Gets or sets the AccrualCodeName.
        /// </summary>
        [XmlAttribute(AttributeName = "AccrualCodeName")]
        public string AccrualCodeName { get; set; }

        /// <summary>
        /// Gets or sets the AccrualType.
        /// </summary>
        [XmlAttribute(AttributeName = "AccrualType")]
        public string AccrualType { get; set; }

        /// <summary>
        /// Gets or sets the EncumberedBalanceInTime.
        /// </summary>
        [XmlAttribute(AttributeName = "EncumberedBalanceInTime")]
        public string EncumberedBalanceInTime { get; set; }

        /// <summary>
        /// Gets or sets the HoursPerDay.
        /// </summary>
        [XmlAttribute(AttributeName = "HoursPerDay")]
        public string HoursPerDay { get; set; }

        /// <summary>
        /// Gets or sets the ProjectedVestedBalanceInTime.
        /// </summary>
        [XmlAttribute(AttributeName = "ProjectedVestedBalanceInTime")]
        public string ProjectedVestedBalanceInTime { get; set; }

        /// <summary>
        /// Gets or sets the ProjectedDate.
        /// </summary>
        [XmlAttribute(AttributeName = "ProjectedDate")]
        public string ProjectedDate { get; set; }

        /// <summary>
        /// Gets or sets the ProjectedGrantAmountInTime.
        /// </summary>
        [XmlAttribute(AttributeName = "ProjectedGrantAmountInTime")]
        public string ProjectedGrantAmountInTime { get; set; }

        /// <summary>
        /// Gets or sets the ProjectedTakingAmountInTime.
        /// </summary>
        [XmlAttribute(AttributeName = "ProjectedTakingAmountInTime")]
        public string ProjectedTakingAmountInTime { get; set; }

        /// <summary>
        /// Gets or sets the VestedBalanceInTime.
        /// </summary>
        [XmlAttribute(AttributeName = "VestedBalanceInTime")]
        public string VestedBalanceInTime { get; set; }
    }
}
