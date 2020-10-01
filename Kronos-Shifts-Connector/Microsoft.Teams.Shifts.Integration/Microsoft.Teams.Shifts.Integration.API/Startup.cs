// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Graph;
    using Microsoft.IdentityModel.Logging;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.JobAssignment;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.PayCodes;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.ShiftsToKronos.CreateTimeOff;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShift;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff;
    using Microsoft.Teams.App.KronosWfc.Service;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Controllers;
    using Microsoft.Teams.Shifts.Integration.API.Extensions;
    using Microsoft.Teams.Shifts.Integration.API.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Polly;
    using Polly.Extensions.Http;

    /// <summary>
    /// Start up file for project.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Configuration object.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets configurations.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure services required.
        /// </summary>
        /// <param name="services">services collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // This line of code will ensure to show what key is being used when it comes to verification of the signature
            // of the incoming JWT token.
            IdentityModelEventSource.ShowPII = true;

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<IKeyVaultHelper, KeyVaultHelper>();
            services.AddSingleton<AppSettings>();
            services.AddSingleton<IApiHelper, ApiHelper>();
            services.AddApplicationInsightsTelemetry();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            var appSettings = serviceProvider.GetService<AppSettings>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = appSettings.RedisCacheConfiguration;
                options.InstanceName = this.Configuration["RedisCacheInstanceName"];
            });

            services.AddHttpClient("ShiftsAPI", c =>
            {
                c.BaseAddress = new Uri(appSettings.GraphApiUrl);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer("Bearer", null);

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "AppID",
                    policy =>
                    {
                        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                        policy.Requirements.Add(new HasAuthRequirement(appSettings.ClientId));
                    });
            });

            services.AddSingleton<IAuthorizationHandler, CustomAuthorize>((provider) => new CustomAuthorize(
                this.Configuration,
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<AppSettings>()));

            services.AddSingleton<IHyperFindActivity, HyperFindActivity>((provider) => new HyperFindActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<BusinessLogic.Providers.IConfigurationProvider>((provider) => new BusinessLogic.Providers.ConfigurationProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton<IUserMappingProvider>((provider) => new UserMappingProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton<IBaseClient, BaseClient>((provider) => new BaseClient(
                appSettings.GraphApiUrl,
                provider.GetRequiredService<IAuthenticationProvider>(),
                null));

            services.AddSingleton<ISwapShiftMappingEntityProvider, SwapShiftMappingEntityProvider>((provider) => new SwapShiftMappingEntityProvider(
             provider.GetRequiredService<TelemetryClient>(),
             appSettings.StorageConnectionString,
             provider.GetRequiredService<IAzureTableStorageHelper>()));

            services.AddSingleton<ILogonActivity, LogonActivity>((provider) => new LogonActivity(
                new App.KronosWfc.Models.RequestEntities.Logon.Request(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<IJobAssignmentActivity, JobAssignmentActivity>((provider) => new JobAssignmentActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<IUpcomingShiftsActivity, UpcomingShiftsActivity>((provider) => new UpcomingShiftsActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<IGraphUtility, GraphUtility>((provider) => new GraphUtility(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider.GetRequiredService<IHttpClientFactory>()));

            services.AddSingleton<IAzureTableStorageHelper, AzureTableStorageHelper>((provider) => new AzureTableStorageHelper(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton((provider) => new SyncKronosToShiftsController(
               provider.GetRequiredService<TelemetryClient>(),
               provider.GetRequiredService<BusinessLogic.Providers.IConfigurationProvider>(),
               provider.GetRequiredService<OpenShiftController>(),
               provider.GetRequiredService<OpenShiftRequestController>(),
               provider.GetRequiredService<SwapShiftController>(),
               provider.GetRequiredService<TimeOffController>(),
               provider.GetRequiredService<TimeOffReasonController>(),
               provider.GetRequiredService<TimeOffRequestsController>(),
               provider.GetRequiredService<ShiftController>(),
               provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton((provider) => new SwapShiftController(
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<ILogonActivity>(),
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<ISwapShiftActivity>(),
                provider.GetRequiredService<ISwapShiftMappingEntityProvider>(),
                provider.GetRequiredService<Utility>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<IShiftMappingEntityProvider>(),
                provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton<IPayCodeActivity, PayCodeActivity>((provider) => new PayCodeActivity(
             provider.GetRequiredService<TelemetryClient>(),
             provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ITimeOffReasonProvider, TimeOffReasonProvider>((provider) => new TimeOffReasonProvider(
                 appSettings.StorageConnectionString,
                 provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton((provider) => new TimeOffReasonController(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IAzureTableStorageHelper>(),
                provider.GetRequiredService<ITimeOffReasonProvider>(),
                provider.GetRequiredService<IPayCodeActivity>(),
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<Utility>()));

            services.AddSingleton((provider) => new ShiftController(
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<IUpcomingShiftsActivity>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<Utility>(),
                provider.GetRequiredService<IShiftMappingEntityProvider>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton<ISwapShiftActivity, SwapShiftActivity>((provider) => new SwapShiftActivity(
             provider.GetRequiredService<TelemetryClient>(),
             provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ILogonActivity, LogonActivity>((provider) => new LogonActivity(
                new App.KronosWfc.Models.RequestEntities.Logon.Request(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ITeamDepartmentMappingProvider, TeamDepartmentMappingProvider>((provider) => new TeamDepartmentMappingProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton<IShiftMappingEntityProvider, ShiftMappingEntityProvider>((provider) => new ShiftMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));

            services.AddSingleton<IOpenShiftMappingEntityProvider, OpenShiftMappingEntityProvider>((provider) => new OpenShiftMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));

            services.AddScoped<ITimeOffActivity, TimeOffActivity>((provider) => new TimeOffActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<BusinessLogic.Providers.IConfigurationProvider>((provider) =>
                new BusinessLogic.Providers.ConfigurationProvider(
                    appSettings.StorageConnectionString,
                    provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton<ITimeOffMappingEntityProvider, TimeOffMappingEntityProvider>((provider) => new TimeOffMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));

            services.AddSingleton<ITimeOffRequestProvider, TimeOffRequestProvider>((provider) => new TimeOffRequestProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));

            services.AddSingleton<IOpenShiftActivity, OpenShiftActivity>((provider) => new OpenShiftActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ICreateTimeOffActivity, CreateTimeOffActivity>((provider) => new CreateTimeOffActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ITimeOffActivity, TimeOffActivity>((provider) => new TimeOffActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));

            services.AddSingleton<ITimeOffReasonProvider>((provider) => new TimeOffReasonProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton<IOpenShiftRequestMappingEntityProvider>((provider) => new OpenShiftRequestMappingEntityProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));

            services.AddSingleton((provider) => new Utility(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<ILogonActivity>(),
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider.GetRequiredService<BusinessLogic.Providers.IConfigurationProvider>(),
                provider.GetRequiredService<IAzureTableStorageHelper>(),
                provider.GetRequiredService<IGraphUtility>()));

            services.AddSingleton((provider) => new OpenShiftController(
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IOpenShiftActivity>(),
                provider.GetRequiredService<Utility>(),
                provider.GetRequiredService<IOpenShiftMappingEntityProvider>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOpenShiftRequestMappingEntityProvider>(),
                provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton((provider) => new OpenShiftRequestController(
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IOpenShiftActivity>(),
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<IOpenShiftRequestMappingEntityProvider>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOpenShiftMappingEntityProvider>(),
                provider.GetRequiredService<Utility>(),
                provider.GetRequiredService<IShiftMappingEntityProvider>()));

            services.AddSingleton((provider) => new TimeOffController(
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<ITimeOffActivity>(),
                provider.GetRequiredService<ITimeOffReasonProvider>(),
                provider.GetRequiredService<IAzureTableStorageHelper>(),
                provider.GetRequiredService<ITimeOffMappingEntityProvider>(),
                provider.GetRequiredService<Utility>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton((provider) => new TimeOffRequestsController(
               provider.GetRequiredService<AppSettings>(),
               provider.GetRequiredService<TelemetryClient>(),
               provider.GetRequiredService<ICreateTimeOffActivity>(),
               provider.GetRequiredService<IUserMappingProvider>(),
               provider.GetRequiredService<ITimeOffReasonProvider>(),
               provider.GetRequiredService<IAzureTableStorageHelper>(),
               provider.GetRequiredService<ITimeOffRequestProvider>(),
               provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
               provider.GetRequiredService<Utility>(),
               provider.GetRequiredService<IHttpClientFactory>(),
               provider.GetRequiredService<BackgroundTaskWrapper>()));

            services.AddSingleton<BackgroundTaskWrapper>();
            services.AddHostedService<Common.BackgroundService>();
            services.AddApplicationInsightsTelemetry();
        }

        /// <summary>
        /// Configure the environment.
        /// </summary>
        /// <param name="app">App builder.</param>
        /// <param name="env">env hosting.</param>
        /// <param name="telemetryClient">Application Insights.</param>
#pragma warning disable CA1822 // Mark members as static
        public void Configure(IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostingEnvironment env, TelemetryClient telemetryClient)
#pragma warning restore CA1822 // Mark members as static
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(telemetryClient);
            app.UseHttpsRedirection();
            app.UseMvc();
        }

        /// <summary>
        /// retry policy.
        /// </summary>
        /// <returns>HttpResponseMessage.</returns>
        internal static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            // Handle both exceptions and return values in one policy
            HttpStatusCode[] httpStatusCodesWorthRetrying =
            {
               HttpStatusCode.RequestTimeout, // 408
               HttpStatusCode.InternalServerError, // 500
               HttpStatusCode.BadGateway, // 502
               HttpStatusCode.GatewayTimeout, // 504
               HttpStatusCode.Forbidden, // 403
            };

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}