// <copyright file="AccountController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Controllers
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The Account controller.
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// The Authentication.
        /// </summary>
        /// <returns>The Authenticated page.</returns>
        public IActionResult SignIn()
        {
            return this.Challenge(new AuthenticationProperties
            {
                RedirectUri = "/",
            });
        }

        /// <summary>
        /// The Signing out.
        /// </summary>
        /// <returns>The redirect URL to get signed out.</returns>
        public IActionResult SignOut()
        {
            return this.SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = "/Account/SignedOut",
                },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// The Signing out.
        /// </summary>
        /// <returns>The Signed out View.</returns>
        public IActionResult SignedOut() => this.View();

        /// <summary>
        /// Having the necessary AccessDenied page.
        /// </summary>
        /// <returns>AccessDenied view.</returns>
        public ActionResult AccessDenied()
        {
            return this.View();
        }
    }
}