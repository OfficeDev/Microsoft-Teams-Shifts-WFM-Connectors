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

        /// <summary>
        /// Initializes a new instance of the <see cref="RetractRequest"/> class.
        /// </summary>
        public RetractRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetractRequest"/> class.
        /// </summary>
        /// <param name="action">The action for the request.</param>
        /// <param name="queryDateSpan">The date span for the request.</param>
        /// <param name="id">The kronos id for the user.</param>
        /// <param name="reqId">The kronos id for the request.</param>
        public RetractRequest(string action, string queryDateSpan, string id, string reqId)
            : this()
        {
            this.Action = action;
            this.EmployeeRequestMgmt = new EmployeeRequestMgmt() { QueryDateSpan = queryDateSpan, Employee = new Employee(id), RequestIds = { Id = reqId } };
        }
    }
}
