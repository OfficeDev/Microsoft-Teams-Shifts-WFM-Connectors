using JdaTeams.Connector.AzureKeyVault.Options;
using JdaTeams.Connector.AzureKeyVault.Services;
using JdaTeams.Connector.AzureStorage.Options;
using JdaTeams.Connector.AzureStorage.Services;
using JdaTeams.Connector.Functions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Http;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.JdaPersona.Services;
using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.MicrosoftGraph;
using JdaTeams.Connector.MicrosoftGraph.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.MicrosoftGraph.Services;
using JdaTeams.Connector.Options;
using JdaTeams.Connector.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace JdaTeams.Connector.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.Services
                .AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>()
                .AddTransient<IScheduleSourceService, JdaPersonaScheduleSourceService>()
                .AddSingleton(config.Get<JdaPersonaOptions>())
                .AddTransient<IScheduleDestinationService, MicrosoftGraphScheduleDestinationService>()
                .AddSingleton(config.Get<MicrosoftGraphOptions>())
                .AddTransient<IScheduleCacheService, AzureStorageScheduleCacheService>()
                .AddSingleton(config.Get<AzureStorageOptions>())
                .AddSingleton(config.Get<TeamOrchestratorOptions>())
                .AddSingleton(config.Get<InitializeOrchestratorOptions>())
                .AddSingleton(config.Get<WeekActivityOptions>())
                .AddSingleton(config.Get<ScheduleActivityOptions>())
                .AddTransient<IAppService, AzureStorageAppService>()
                .AddTransient<IScheduleConnectorService, AzureStorageScheduleConnectorService>()
                .AddTransient<ITimeZoneService, AzureStorageTimeZoneService>()
                .AddSingleton<ISecretsService, AzureKeyVaultSecretsService>()
                .AddSingleton(config.Get<AzureKeyVaultOptions>())
                .AddSingleton<IScheduleDeltaService, DefaultScheduleDeltaService>()
                .AddSingleton<IMicrosoftGraphClientFactory, MicrosoftGraphClientFactory>()
                .AddSingleton<IUserPrincipalMap, MicrosoftGraphUserPrincipalMap>()
                .AddSingleton<IShiftThemeMap, MicrosoftGraphShiftThemeMap>()
                .AddSingleton(config.Get<ClearScheduleOptions>())
                .AddSingleton<IShiftMap, MicrosoftGraphShiftMap>()
                .AddSingleton(config.Get<ConnectorOptions>());

            builder.UseHttpOptions(config.Get<HttpOptions>());
            builder.UseTracingOptions(config.Get<TracingOptions>());

        }
    }
}
