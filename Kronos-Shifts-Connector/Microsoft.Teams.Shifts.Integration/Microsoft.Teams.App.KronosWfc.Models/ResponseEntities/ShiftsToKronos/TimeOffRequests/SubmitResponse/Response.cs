// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.SubmitResponse
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Time off requests response.
    /// </summary>
    [XmlRoot]
    public class Response
    {
        /// <summary>
        /// Gets or sets the Status of response.
        /// </summary>
        [XmlAttribute]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the Action of response.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Error Response.
        /// </summary>
        public Error Error { get; set; }
    }
}