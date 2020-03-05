// <copyright file="PayCode.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.PayCodes
{
    using System.Xml.Serialization;

    /// <summary>
    /// Paycode class.
    /// </summary>
    [XmlRoot(ElementName = "PayCode")]
    public class PayCode
    {
        /// <summary>
        /// Gets or sets displayOrder of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "DisplayOrder")]
        public string DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets ExcuseAbsenceFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "ExcuseAbsenceFlag")]
        public string ExcuseAbsenceFlag { get; set; }

        /// <summary>
        /// Gets or sets IsCombinedFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "IsCombinedFlag")]
        public string IsCombinedFlag { get; set; }

        /// <summary>
        /// Gets or sets IsCurrencyFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "IsCurrencyFlag")]
        public string IsCurrencyFlag { get; set; }

        /// <summary>
        /// Gets or sets IsVisibleFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "IsVisibleFlag")]
        public string IsVisibleFlag { get; set; }

        /// <summary>
        /// Gets or sets ManagerAccessFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "ManagerAccessFlag")]
        public string ManagerAccessFlag { get; set; }

        /// <summary>
        /// Gets or sets PayCodeName of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "PayCodeName")]
        public string PayCodeName { get; set; }

        /// <summary>
        /// Gets or sets ProfessionalAccessFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "ProfessionalAccessFlag")]
        public string ProfessionalAccessFlag { get; set; }

        /// <summary>
        /// Gets or sets AffectsAvailability of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "AffectsAvailability")]
        public string AffectsAvailability { get; set; }

        /// <summary>
        /// Gets or sets IsDaysFlag of paycode.
        /// </summary>
        [XmlAttribute(AttributeName = "IsDaysFlag")]
        public string IsDaysFlag { get; set; }
    }
}
