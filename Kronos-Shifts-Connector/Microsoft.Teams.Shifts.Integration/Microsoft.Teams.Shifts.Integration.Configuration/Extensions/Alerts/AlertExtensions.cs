// <copyright file="AlertExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Extensions.Alerts
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// This class functions for the necessary alerts to be shown.
    /// </summary>
    public static class AlertExtensions
    {
        /// <summary>
        /// Renders a successful alert.
        /// </summary>
        /// <param name="result">The IActionResult.</param>
        /// <param name="title">The title of the alert.</param>
        /// <param name="body">The content of the alert.</param>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public static IActionResult WithSuccess(this IActionResult result, string title, string body)
        {
            return Alert(result, "success", title, body);
        }

        /// <summary>
        /// Renders an alert with information.
        /// </summary>
        /// <param name="result">The IActionResult.</param>
        /// <param name="title">The title of the alert.</param>
        /// <param name="body">The content of the alert.</param>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public static IActionResult WithInfo(this IActionResult result, string title, string body)
        {
            return Alert(result, "info", title, body);
        }

        /// <summary>
        /// Renders an alert with a warning.
        /// </summary>
        /// <param name="result">The IActionResult.</param>
        /// <param name="title">The title of the alert.</param>
        /// <param name="body">The content of the alert.</param>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public static IActionResult WithWarning(this IActionResult result, string title, string body)
        {
            return Alert(result, "warning", title, body);
        }

        /// <summary>
        /// Renders an alert with error message.
        /// </summary>
        /// <param name="result">The IActionResult.</param>
        /// <param name="title">The title of the alert.</param>
        /// <param name="body">The content of the alert.</param>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public static IActionResult WithErrorMessage(this IActionResult result, string title, string body)
        {
            return Alert(result, "danger", title, body);
        }

        private static IActionResult Alert(IActionResult result, string type, string title, string body)
        {
            return new AlertDecoratorResult(result, type, title, body);
        }
    }
}