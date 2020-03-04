// <copyright file="PersonIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline
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
        [XmlAttribute]
        public string PersonNumber { get; set; }
    }
}