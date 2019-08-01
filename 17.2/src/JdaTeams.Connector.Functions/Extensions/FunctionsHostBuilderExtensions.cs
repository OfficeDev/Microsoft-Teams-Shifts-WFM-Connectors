using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Functions.Tracing;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Rest;
using System.Net;

namespace JdaTeams.Connector.Functions.Extensions
{
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
