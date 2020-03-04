// <copyright file="AlertDecoratorResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Extensions.Alerts
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// This is the AlertDecoratorResult class.
    /// </summary>
    public class AlertDecoratorResult : IActionResult
    {
        private readonly IActionResult result;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertDecoratorResult"/> class.
        /// </summary>
        /// <param name="result">The IActionResult.</param>
        /// <param name="type">The type of result/alert.</param>
        /// <param name="title">The title of the alert.</param>
        /// <param name="body">The content of the alert.</param>
        public AlertDecoratorResult(
            IActionResult result,
            string type,
            string title,
            string body)
        {
            this.result = result;
            this.Type = type;
            this.Title = title;
            this.Body = body;
        }

        private string Type { get; }

        private string Title { get; }

        private string Body { get; }

        /// <summary>
        /// Method to execute the result.
        /// </summary>
        /// <param name="context">The ActionContext.</param>
        /// <returns>A unit of execution.</returns>
        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var factory = context.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory>();

            var tempData = factory.GetTempData(context.HttpContext);
            tempData["_alert.type"] = this.Type;
            tempData["_alert.title"] = this.Title;
            tempData["_alert.body"] = this.Body;

            await this.result.ExecuteResultAsync(context).ConfigureAwait(false);
        }
    }
}