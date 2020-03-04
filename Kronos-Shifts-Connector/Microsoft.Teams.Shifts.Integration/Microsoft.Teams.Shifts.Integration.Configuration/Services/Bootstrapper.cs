// <copyright file="Bootstrapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Services
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// This class will bootstrap the ability to authenticate against graph.
    /// </summary>
    public static class Bootstrapper
    {
        /// <summary>
        /// Method to configure the necessary options.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="configuration">The Key/Value application configurations.</param>
        public static void AddGraphService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<WebOptions>(configuration);
        }
    }
}