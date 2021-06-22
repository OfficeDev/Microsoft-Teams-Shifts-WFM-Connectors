// <copyright file="Person.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// Models the Person tag.
    /// </summary>
    public class Person
    {
        /// <summary>
        /// Gets or Sets the Person Name.
        /// </summary>
        [XmlAttribute]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or Sets the Person Number.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set; }
    }
}