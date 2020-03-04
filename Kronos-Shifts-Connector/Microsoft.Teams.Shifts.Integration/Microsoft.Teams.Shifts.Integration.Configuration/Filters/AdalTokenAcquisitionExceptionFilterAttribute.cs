// <copyright file="AdalTokenAcquisitionExceptionFilterAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Filters
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Triggers authentication if access token cannot be acquired
    /// silently, i.e. from cache.
    /// </summary>
    public class AdalTokenAcquisitionExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <summary>
        /// Re-Authentication method.
        /// </summary>
        /// <param name="context">Exception Context if authentication fails.</param>
        public override void OnException(ExceptionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // If ADAL failed to acquire access token
            if (context.Exception is AdalSilentTokenAcquisitionException)
            {
                // Send user to Azure AD to re-authenticate
                context.Result = new ChallengeResult();
            }
        }
    }
}