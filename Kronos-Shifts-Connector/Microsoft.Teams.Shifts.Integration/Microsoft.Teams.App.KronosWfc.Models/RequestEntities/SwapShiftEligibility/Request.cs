// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShiftEligibility
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// This class models the Request.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class Request
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

        /// <summary>
        /// Gets or sets the Swap Shift Employees tag.
        /// </summary>
        [XmlElement]
        public SwapShiftEmployees SwapShiftEmployees { get; set; }
    }
}