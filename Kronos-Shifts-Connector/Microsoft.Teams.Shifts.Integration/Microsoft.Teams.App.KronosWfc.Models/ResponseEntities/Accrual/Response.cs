// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Accrual
{
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create top level placeholder to wrap actual Accrual response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the Action for the request.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets Error response.
        /// </summary>
        public Error Error { get; set; }

        /// <summary>
        /// Gets or sets the Status.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the AccrualData request.
        /// </summary>
        public AccrualData AccrualData { get; set; }
    }
}