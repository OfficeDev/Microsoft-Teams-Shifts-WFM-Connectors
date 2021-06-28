// <copyright file="Response.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShiftEligibility
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common;

    /// <summary>
    /// This class models the response.
    /// </summary>
    [XmlRoot]
    public class Response : Common.Response
    {
        /// <summary>
        /// Gets or Sets the Person Information from Kronos.
        /// </summary>
        [XmlElement]
        public Person Person { get; set; }
    }
}