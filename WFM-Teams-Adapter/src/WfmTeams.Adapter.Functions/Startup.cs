// ---------------------------------------------------------------------------
// <copyright file="Startup.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using WfmTeams.Adapter.AzureKeyVault.Options;
using WfmTeams.Adapter.AzureKeyVault.Services;
using WfmTeams.Adapter.AzureRedis.Options;
using WfmTeams.Adapter.AzureRedis.Services;
using WfmTeams.Adapter.AzureStorage.Options;
using WfmTeams.Adapter.AzureStorage.Services;
using WfmTeams.Adapter.Functions;
using WfmTeams.Adapter.Functions.Extensions;
using WfmTeams.Adapter.Functions.Handlers;
using WfmTeams.Adapter.Functions.Localization;
using WfmTeams.Adapter.Functions.Options;
using WfmTeams.Adapter.Functions.Triggers;
using WfmTeams.Adapter.Http;
using WfmTeams.Adapter.Mappings;
using WfmTeams.Adapter.MicrosoftGraph;
using WfmTeams.Adapter.MicrosoftGraph.Mappings;
using WfmTeams.Adapter.MicrosoftGraph.Options;
using WfmTeams.Adapter.MicrosoftGraph.Services;
using WfmTeams.Adapter.Models;
using WfmTeams.Adapter.Options;
using WfmTeams.Adapter.Services;
using WfmTeams.Connector.BlueYonder.Services;

[assembly: FunctionsStartup(typeof(Startup))]

namespace WfmTeams.Adapter.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.Services
                .AddSingleton<IStringLocalizer<ChangeRequestTrigger>, EmbeddedStringLocalizer<ChangeRequestTrigger>>()
                .AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>()
                .AddTransient<ISecretsService, AzureKeyVaultSecretsService>()
                .AddTransient<ITeamsService, TeamsService>()
                .AddSingleton(config.Get<MicrosoftGraphOptions>())
                .AddTransient<IScheduleCacheService, AzureStorageScheduleCacheService>()
                .AddTransient<IRequestCacheService, AzureStorageRequestCacheService>()
                .AddTransient<ITimeOffCacheService, AzureStorageTimeOffCacheService>()
                .AddSingleton(config.Get<AzureStorageOptions>())
                .AddSingleton(config.Get<TeamOrchestratorOptions>())
                .AddSingleton(config.Get<InitializeOrchestratorOptions>())
                .AddSingleton(config.Get<WeekActivityOptions>())
                .AddSingleton(config.Get<ScheduleActivityOptions>())
                .AddTransient<IAppService, AzureStorageAppService>()
                .AddTransient<IScheduleConnectorService, AzureStorageScheduleConnectorService>()
                .AddTransient<ITimeZoneService, AzureStorageTimeZoneService>()
                .AddSingleton<IDeltaService<ShiftModel>, DefaultScheduleDeltaService>()
                .AddSingleton<IDeltaService<TimeOffModel>, DefaultTimeOffDeltaService>()
                .AddSingleton<IDeltaService<EmployeeAvailabilityModel>, DefaultAvailabilityDeltaService>()
                .AddSingleton<IMicrosoftGraphClientFactory, MicrosoftGraphClientFactory>()
                .AddSingleton<IUserPrincipalMap, MicrosoftGraphUserPrincipalMap>()
                .AddSingleton<IShiftThemeMap, MicrosoftGraphShiftThemeMap>()
                .AddSingleton(config.Get<ClearScheduleOptions>())
                .AddSingleton<IShiftMap, MicrosoftGraphShiftMap>()
                .AddSingleton(config.Get<ConnectorOptions>())
                .AddSingleton(config.Get<WorkforceIntegrationOptions>())
                .AddSingleton<ISystemTimeService, DefaultSystemTimeService>()
                .AddSingleton<IChangeRequestHandler, CancelSwapRequestHandler>()
                .AddSingleton<IChangeRequestHandler, SenderSwapRequestHandler>()
                .AddSingleton<IChangeRequestHandler, RecipientSwapRequestHandler>()
                .AddSingleton<IChangeRequestHandler, ManagerSwapRequestHandler>()
                .AddSingleton<IChangeRequestHandler, CancelOpenShiftRequestHandler>()
                .AddSingleton<IChangeRequestHandler, SenderOpenShiftRequestHandler>()
                .AddSingleton<IChangeRequestHandler, ManagerOpenShiftRequestHandler>()
                .AddSingleton<IChangeRequestHandler, ShiftChangeHandler>()
                .AddSingleton<IChangeRequestHandler, OpenShiftChangeHandler>()
                .AddSingleton<IChangeRequestHandler, ShiftPreferenceChangeRequestHandler>()
                .AddSingleton<IChangeRequestHandler, ManagerAssignOpenShiftHandler>()
                .AddSingleton<IChangeRequestHandler, ShiftSwapFilterHandler>()
                .AddSingleton(config.Get<AzureRedisOptions>())
                .AddSingleton<ICacheService, AzureRedisCacheService>()
                .AddSingleton<ITimeOffMap, MicrosoftGraphTimeOffMap>()
                .AddSingleton(config.Get<ConnectorHealthOptions>())
                .AddSingleton(config.Get<AzureKeyVaultOptions>())
                .AddSingleton<MicrosoftGraph.Mappings.IAvailabilityMap, MicrosoftGraphAvailabilityMap>()
                .AddSingleton(config.Get<FeatureOptions>());

            var connectorOptions = config.Get<ConnectorOptions>();
            IWfmConfigService configService = null;
            switch (connectorOptions.WfmProvider)
            {
                case ProviderType.BlueYonder:
                {
                    configService = ConfigureForBlueYonder(builder.Services);
                    break;
                }
            }

            configService.ConfigureServices(builder.Services, config);

            builder.UseHttpOptions(config.Get<HttpOptions>())
                .UseTracingOptions(config.Get<TracingOptions>());
        }

        private IWfmConfigService ConfigureForBlueYonder(IServiceCollection services)
        {
            services.AddTransient<IWfmDataService, BlueYonderDataService>()
                .AddTransient<IWfmActionService, BlueYonderActionService>()
                .AddTransient<IWfmAuthService, BlueYonderAuthService>();

            return new BlueYonderConfigService();
        }
    }
}
