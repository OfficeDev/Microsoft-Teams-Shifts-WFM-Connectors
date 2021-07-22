// <copyright file="RequestId.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// Model for the RequestId tag.
    /// </summary>
    public class RequestId
    {
        [XmlAttribute]
        public string Id { get; set; }
    }
}
