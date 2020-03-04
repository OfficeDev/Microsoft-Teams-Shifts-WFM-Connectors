// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the response.
    /// </summary>
    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        /// <summary>
        /// Gets or sets the RequestMgmt.
        /// </summary>
        [XmlElement(ElementName = "RequestMgmt")]
        public RequestMgmt RequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}