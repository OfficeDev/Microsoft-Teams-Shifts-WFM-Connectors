// <copyright file="AccruasResponsel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// The AccrualResponse tag.
    /// </summary>
    [XmlRoot(ElementName = "AccrualResponse")]
    public class AccrualResponse
    {
        /// <summary>
        /// Gets or sets the HasEnoughBalance.
        /// </summary>
        [XmlElement(ElementName = "HasEnoughBalance")]
        public bool HasEnoughBalance { get; set; }

        // <summary>
        /// Gets or sets the error message.
        /// </summary>
        [XmlElement(ElementName = "ErrorMessage")]
        public string ErrorMessage { get; set; }
    }
}
