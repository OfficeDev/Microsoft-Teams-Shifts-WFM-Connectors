// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the user.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the PersonIdentity.
        /// </summary>
        [XmlElement(ElementName = "PersonIdentity")]
        public RequestEntities.JobAssignment.PersonIdentity PersonIdentity { get; set; }
    }
}