// <copyright file="ErrorArr.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create the top level placeholder to wrap all error messages.
    /// </summary>
    public class ErrorArr
    {
        /// <summary>
        /// Gets or sets the list of Errors.
        /// </summary>
        [XmlElement]
#pragma warning disable CA1819 // Properties should not return arrays
        public Error[] Error { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}