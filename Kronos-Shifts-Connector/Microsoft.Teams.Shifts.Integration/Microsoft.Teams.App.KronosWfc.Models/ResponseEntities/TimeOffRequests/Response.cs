// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Response.
    /// </summary>
    [XmlRoot(ElementName = "Response")]
    public class Response
    {
        /// <summary>
        /// Gets or sets the RequestMgmt entity.
        /// </summary>
        [XmlElement(ElementName = "RequestMgmt")]
        public RequestMgmt RequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute(AttributeName = "Status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        public Error Error { get; set; }
    }
}