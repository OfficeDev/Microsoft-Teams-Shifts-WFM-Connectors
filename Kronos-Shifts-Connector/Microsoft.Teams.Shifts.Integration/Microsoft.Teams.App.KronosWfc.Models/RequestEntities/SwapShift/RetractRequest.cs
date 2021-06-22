// <copyright file="RetractRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// Model representing the request needed to retract/cancel a shift swap.
    /// </summary>
    [XmlRoot(ElementName = "Request")]
    public class RetractRequest
    {
        /// <summary>
        /// Gets or Sets the employee request tag.
        /// </summary>
        [XmlElement]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}
