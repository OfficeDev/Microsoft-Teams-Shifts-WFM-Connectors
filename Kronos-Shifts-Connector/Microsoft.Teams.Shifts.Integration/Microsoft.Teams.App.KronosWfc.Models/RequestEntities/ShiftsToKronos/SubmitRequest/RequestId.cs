// <copyright file="RequestId.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Submit request Id.
    /// </summary>
    public class RequestId
    {
        /// <summary>
        /// Gets or sets submit request Id.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }
    }
}
