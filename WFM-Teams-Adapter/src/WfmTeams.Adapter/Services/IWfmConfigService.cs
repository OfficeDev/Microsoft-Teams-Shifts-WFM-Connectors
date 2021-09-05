// ---------------------------------------------------------------------------
// <copyright file="IWfmConfigService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public interface IWfmConfigService
    {
        /// <summary>
        /// Gives the WFM Connector the ability to add it's only dependencies to the service collection.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="config">
        /// The configuration root object to use when getting its connector specific settings.
        /// </param>
        void ConfigureServices(IServiceCollection services, IConfigurationRoot config);
    }
}
