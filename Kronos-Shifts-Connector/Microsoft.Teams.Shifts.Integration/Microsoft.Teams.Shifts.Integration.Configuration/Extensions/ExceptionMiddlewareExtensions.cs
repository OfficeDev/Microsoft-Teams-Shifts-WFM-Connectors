// <copyright file="ExceptionMiddlewareExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Extensions
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Teams.Shifts.Integration.API.Models;

    /// <summary>
    /// This class is the custom extension middleware.
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Establishes the exception handler to handle all exceptions.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="telemetryClient">Application Insights.</param>
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, TelemetryClient telemetryClient)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    Dictionary<string, string> exceptionDictionary = new Dictionary<string, string>();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null && contextFeature.Error != null)
                    {
                        exceptionDictionary.Add("ErrorMessage", contextFeature?.Error?.Message);
                        telemetryClient.TrackException(contextFeature.Error, exceptionDictionary);

                        await context.Response.WriteAsync(new ErrorDetails()
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = contextFeature?.Error?.Message,
                        }.ToString()).ConfigureAwait(false);
                    }
                });
            });
        }
    }
}