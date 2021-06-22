// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.TimeOffApproveDecline
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the PersonIdentity.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        [XmlAttribute(AttributeName = "PersonNumber")]
        public string PersonNumber { get; set; }
    }
}