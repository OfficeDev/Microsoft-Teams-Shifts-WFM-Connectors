// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;

    /// <summary>
    /// This class models the Response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the EmployeeRequestMgmt.
        /// </summary>
        [XmlElement("EmployeeRequestMgmt")]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the sequence.
        /// </summary>
        [XmlAttribute]
        public string Sequence { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
    }

    // public class EmployeeRequestMgmt
    // {
    //     [XmlAttribute]
    //     public string QueryDateSpan { get; set; }
    //     [XmlElement("Employee")]
    //     public Employee Employees { get; set; }
    //     [XmlElement("RequestItems")]
    //     public RequestItems RequestItem { get; set; }
    // }
    // public class RequestItems
    // {
    //     [XmlElement("SwapShiftRequestItem")]
    //     public EmployeeSwapShiftRequestItems[] EmployeeSwapShiftRequestItems { get; set; }
    // }
    // public class EmployeeSwapShiftRequestItems
    // {
    //    public EmployeeSwapShiftRequestItem[] EmployeeSwapShiftRequestItem { get; set; }
    // }

    // public class EmployeeSwapShiftRequestItem {
    //     [XmlAttribute]
    //     public string Id { get; set; }
    //     [XmlAttribute]
    //     public string RequestFor { get; set; }
    //     [XmlAttribute]
    //     public string DateTime { get; set; }
    //     [XmlElement]
    //     public OfferedShift OfferedShift { get; set; }
    //     [XmlElement]
    //     public RequestedShift RequestedShift { get; set; }
    // }
    // public class OfferedShift
    // {
    //     [XmlElement]
    //     public ShiftRequestItem ShiftRequestItem { get; set; }
    // }
    // public class ShiftRequestItem
    // {
    //     [XmlAttribute]
    //     public string StartDateTime { get; set; }
    //     [XmlAttribute]
    //     public string EndDateTime { get; set; }
    //     [XmlAttribute]
    //     public string OrgJobPath { get; set; }
    //     [XmlElement]
    //     public Employee Employee { get; set; }
    // }
    // public class RequestedShift
    // {
    //     [XmlElement]
    //     public ShiftRequestItem ShiftRequestItem { get; set; }
    // }
}