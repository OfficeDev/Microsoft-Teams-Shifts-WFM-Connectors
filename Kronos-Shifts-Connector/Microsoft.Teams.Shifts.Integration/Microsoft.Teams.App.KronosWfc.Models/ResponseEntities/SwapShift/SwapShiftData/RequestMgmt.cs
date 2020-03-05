// <copyright file="RequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestMgmt.
    /// </summary>
    [XmlRoot(ElementName = "RequestMgmt")]
    public class RequestMgmt
    {
        /// <summary>
        /// Gets or sets the RequestItems.
        /// </summary>
        [XmlElement(ElementName = "RequestItems")]
        public RequestItems RequestItems { get; set; }

        /// <summary>
        /// Gets or sets the employees.
        /// </summary>
        [XmlElement(ElementName = "Employees")]
        public Employees Employees { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }
    }
}