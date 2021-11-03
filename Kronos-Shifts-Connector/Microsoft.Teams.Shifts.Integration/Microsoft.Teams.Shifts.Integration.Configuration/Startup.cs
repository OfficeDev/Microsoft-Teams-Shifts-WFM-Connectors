// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFindLoadAll;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.JobAssignment;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.Service;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Extensions;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Cache;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.Configuration.Controllers;
    using Microsoft.Teams.Shifts.Integration.Configuration.Filters;
    using Microsoft.Teams.Shifts.Integration.Configuration.Models;
    using Microsoft.Teams.Shifts.Integration.Configuration.Services;
    using Polly;
    using Polly.Extensions.Http;

    /// <summary>
    /// Having the startup file - sets all of the dependencies required.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Key/value configuration properties.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Mechanisms to configure the application request pipeline.</param>
        /// <param name="env">Web hosting environment that the application is executing inside of.</param>
        /// <param name="telemetryClient">Application Insights for logging and telemetry.</param>
        public static void Configure(IApplicationBuilder app, IHostingEnvironment env, TelemetryClient telemetryClient)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(telemetryClient);
            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(opts =>
            {
                opts.Filters.Add(typeof(AdalTokenAcquisitionExceptionFilterAttribute));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddDataProtection();

            // Wiring up App Insights
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<IKeyVaultHelper, KeyVaultHelper>();
            services.AddSingleton<AppSettings>();
            services.AddSingleton<IApiHelper, ApiHelper>();

            var serviceProvider = services.BuildServiceProvider();
            var appSettings = serviceProvider.GetService<AppSettings>();

            // Add a strongly-typed options class to DI
            services.Configure<AuthOptionsModel>(opt =>
            {
                opt.Authority = appSettings.Authority;
                opt.ClientId = appSettings.ClientId;
                opt.ClientSecret = appSettings.ClientSecret;
            });

            services.AddScoped<ITokenCacheFactory, TokenCacheFactory>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = appSettings.RedisCacheConfiguration;
                options.InstanceName = this.Configuration["RedisCacheInstanceName"];
            });

            services.AddHttpClient("ShiftsKronosIntegrationAPI", c =>
            {
                c.BaseAddress = new Uri(this.Configuration["BaseAddressFirstTimeSync"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient("GraphBetaAPI", client =>
            {
                client.BaseAddress = new Uri(this.Configuration["GraphApiUrl"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                auth.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(opts =>
            {
                opts.SlidingExpiration = true;
                opts.AccessDeniedPath = new PathString("/Account/AccessDenied");
            })
            .AddOpenIdConnect(
                opts =>
            {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                opts.Events.OnTicketReceived = async (context) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                    context.Properties.ExpiresUtc = DateTime.UtcNow.AddHours(1);
                };

                opts.ClientId = this.Configuration["ClientId"];
                opts.ClientSecret = this.Configuration["ClientSecret"];
                opts.Authority = this.Configuration["Authority"];
                opts.ResponseType = this.Configuration["ResponseType"];

                this.Configuration.Bind(opts);
                opts.TokenValidationParameters.ValidateIssuer = false;
                opts.Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async ctx =>
                    {
                        HttpRequest request = ctx.HttpContext.Request;

                        // We need to also specify the redirect URL used
                        string currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);

                        // Credentials for app itself
                        var credential = new ClientCredential(appSettings.ClientId, appSettings.ClientSecret);

                        // Construct token cache
                        var distributedCache = ctx.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                        var cache = new RedisTokenCache(distributedCache, appSettings.ClientId);

                        var authContext = new AuthenticationContext("https://login.microsoftonline.com/common", cache);

                        // Get token for Microsoft Graph API using the authorization code
                        string resource = "https://graph.microsoft.com";
                        AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                            ctx.ProtocolMessage.Code, new Uri(currentUri), credential, resource).ConfigureAwait(false);

                        // Tell the OIDC middleware we got the tokens, it doesn't need to do anything
                        ctx.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    },
                };
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddOptions();
            services.AddHttpClient();

            // As each sub-integration is being implemented, we need to make sure that we can correctly setup the DI.g
            services.AddSingleton<BusinessLogic.Providers.IConfigurationProvider>((provider) => new BusinessLogic.Providers.ConfigurationProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));
            services.AddSingleton<ITeamDepartmentMappingProvider>((provider) => new TeamDepartmentMappingProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));
            services.AddSingleton<IUserMappingProvider>((provider) => new UserMappingProvider(
                appSettings.StorageConnectionString,
                provider.GetRequiredService<TelemetryClient>()));
            services.AddSingleton<IGraphUtility>((provider) => new GraphUtility(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));
            services.AddSingleton<ShiftsTeamKronosDepartmentViewModel>();
            services.AddSingleton<IShiftMappingEntityProvider>((provider) => new ShiftMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));
            services.AddSingleton<IOpenShiftMappingEntityProvider>((provider) => new OpenShiftMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));
            services.AddSingleton<ITimeOffMappingEntityProvider>((provider) => new TimeOffMappingEntityProvider(
                provider.GetRequiredService<TelemetryClient>(),
                appSettings.StorageConnectionString));
            services.AddSingleton<IAzureTableStorageHelper>((provider) => new AzureTableStorageHelper(
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

            services.AddSingleton((provider) => new TeamDepartmentMappingController(
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<ILogonActivity>(),
                provider.GetRequiredService<IHyperFindLoadAllActivity>(),
                provider.GetRequiredService<ShiftsTeamKronosDepartmentViewModel>(),
                provider.GetRequiredService<IGraphUtility>(),
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<BusinessLogic.Providers.IConfigurationProvider>(),
                provider.GetRequiredService<IDistributedCache>(),
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));

            services.AddSingleton((provider) => new UserMappingController(
                provider.GetRequiredService<AppSettings>(),
                provider.GetRequiredService<IGraphUtility>(),
                provider.GetRequiredService<ILogonActivity>(),
                provider.GetRequiredService<IHyperFindActivity>(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IUserMappingProvider>(),
                provider.GetRequiredService<ITeamDepartmentMappingProvider>(),
                provider.GetRequiredService<BusinessLogic.Providers.IConfigurationProvider>(),
                provider.GetRequiredService<IJobAssignmentActivity>(),
                provider.GetRequiredService<IHostingEnvironment>(),
                provider.GetRequiredService<Utility>()));

            // Wiring up Kronos dependency chain to set up DI container.
            services.AddSingleton<App.KronosWfc.Models.RequestEntities.Logon.Request>();
            services.AddSingleton<ILogonActivity, LogonActivity>((provider) => new LogonActivity(
                new App.KronosWfc.Models.RequestEntities.Logon.Request(),
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));
            services.AddSingleton<IHyperFindLoadAllActivity, HyperFindLoadAllActivity>((provider) => new HyperFindLoadAllActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));
            services.AddSingleton<IHyperFindActivity, HyperFindActivity>((provider) => new HyperFindActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));
            services.AddSingleton<IJobAssignmentActivity, JobAssignmentActivity>((provider) => new JobAssignmentActivity(
                provider.GetRequiredService<TelemetryClient>(),
                provider.GetRequiredService<IApiHelper>()));
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