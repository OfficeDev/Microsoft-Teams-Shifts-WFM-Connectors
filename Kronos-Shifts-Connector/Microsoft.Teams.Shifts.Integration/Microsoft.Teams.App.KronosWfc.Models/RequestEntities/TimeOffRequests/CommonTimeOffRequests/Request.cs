// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.CommonTimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class Request
    {
        /// <summary>
        /// Gets or sets the RequestMgmt object.
        /// </summary>
        [XmlElement(ElementName = "RequestMgmt")]
        public RequestMgmt RequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}