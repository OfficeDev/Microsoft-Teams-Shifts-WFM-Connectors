// ---------------------------------------------------------------------------
// <copyright file="FunctionsHostBuilderExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Extensions
{
    using System.Net;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Rest;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Tracing;

    public static class FunctionsHostBuilderExtensions
    {
        public static IFunctionsHostBuilder UseHttpOptions(this IFunctionsHostBuilder builder, HttpOptions options)
        {
            if (options.IgnoreCertificateValidation)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;
            }

            return builder;
        }

        public static IFunctionsHostBuilder UseTracingOptions(this IFunctionsHostBuilder builder, TracingOptions options)
        {
            if (options.TraceEnabled)
            {
                ServiceClientTracing.AddTracingInterceptor(new AutoRestTracingInterceptor(options));
                ServiceClientTracing.IsEnabled = true;
            }

            return builder;
        }
    }
}
