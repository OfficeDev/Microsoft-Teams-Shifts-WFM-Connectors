// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Accrual
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to create top level placeholder to wrap actual Accrual requests.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the Action for the request.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the AccrualData request.
        /// </summary>
        public AccrualData AccrualData { get; set; }
    }
}