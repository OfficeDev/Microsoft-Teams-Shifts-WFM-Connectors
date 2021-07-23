// <copyright file="CreateOpenShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the createOpenShift request.
    /// </summary>
    [XmlRoot("Request")]
    public class CreateOpenShift
    {
        /// <summary>
        /// Gets or sets the openShiftSchedule.
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
        public OpenShiftSchedule Schedule { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute(AttributeName = "Action")]
        public string Action { get; set; }
    }
}