// <copyright file="HasAuthRequirement.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models
{
    using System;
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// Auth requirment paramenters.
    /// </summary>
    public class HasAuthRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HasAuthRequirement"/> class.
        /// Check if AppId is provided.
        /// </summary>
        /// <param name="appID">Service AppId.</param>
        public HasAuthRequirement(string appID)
        {
            this.AppID = appID ?? throw new ArgumentNullException(nameof(appID));
        }

        /// <summary>
        /// Gets app Id of the service.
        /// </summary>
        public string AppID { get; }
    }
}
