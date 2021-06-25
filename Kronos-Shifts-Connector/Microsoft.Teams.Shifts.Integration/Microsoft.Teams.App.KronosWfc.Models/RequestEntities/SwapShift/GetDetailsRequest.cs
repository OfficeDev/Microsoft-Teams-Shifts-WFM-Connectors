// <copyright file="GetDetailsRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift
{
    using System.Xml.Serialization;
    using Models.RequestEntities.Common;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class GetDetailsRequest
    {
        /// <summary>
        /// Gets or sets the RequestMgmt object.
        /// </summary>
        [XmlElement]
        public RequestMgmt RequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}