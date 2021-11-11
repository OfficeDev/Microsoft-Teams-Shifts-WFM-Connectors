// ---------------------------------------------------------------------------
// <copyright file="BlueYonderConfigService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Localization;
    using WfmTeams.Connector.BlueYonder.Mappings;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderConfigService : IWfmConfigService
    {
        public void ConfigureServices(IServiceCollection services, IConfigurationRoot config)
        {
            services.AddSingleton<IStringLocalizer<BlueYonderConfigService>, EmbeddedStringLocalizer<BlueYonderConfigService>>()
                .AddSingleton<IBlueYonderClientFactory, BlueYonderClientFactory>()
                .AddSingleton<IAvailabilityMap, BlueYonderAvailabilityMap>()
                .AddSingleton<IJwtTokenService, BlueYonderJwtTokenService>()
                .AddSingleton(config.Get<BlueYonderPersonaOptions>());
        }
    }
}
