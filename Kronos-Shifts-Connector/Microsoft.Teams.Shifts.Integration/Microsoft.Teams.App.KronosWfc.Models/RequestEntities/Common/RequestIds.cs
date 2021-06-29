// <copyright file="RequestIds.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Model representing a list of Request Ids.
    /// </summary>
    public class RequestIds
    {
        /// <summary>
        /// Gets or Sets the Id for a request.
        /// </summary>
        [XmlElement]
        public List<RequestId> RequestId { get; set; }
    }
}
