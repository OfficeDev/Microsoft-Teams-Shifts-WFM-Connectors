// <copyright file="PersonIdentityTag.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// The PersonIdentity tag.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or Sets the Person Number attribute.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set;  }
    }
}
