// <copyright file="RequestId.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestId.
    /// </summary>
    public class RequestId
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }
    }
}