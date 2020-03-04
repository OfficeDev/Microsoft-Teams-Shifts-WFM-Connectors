// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestItems.
    /// </summary>
    [XmlRoot(ElementName = "RequestItems")]
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets the EmployeeGlobalOpenShiftRequestItem.
        /// </summary>
        [XmlElement(ElementName = "EmployeeGlobalOpenShiftRequestItem")]
        public EmployeeGlobalOpenShiftRequestItem EmployeeGlobalOpenShiftRequestItem { get; set; }
    }
}