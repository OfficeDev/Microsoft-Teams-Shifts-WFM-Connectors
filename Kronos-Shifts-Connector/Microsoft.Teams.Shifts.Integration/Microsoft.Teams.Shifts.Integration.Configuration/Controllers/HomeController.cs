// <copyright file="HomeController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions.Alerts;
    using Microsoft.Teams.Shifts.Integration.Configuration.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// The home controller.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly TelemetryClient telemetryClient;
        private readonly IGraphUtility graphUtility;
        private readonly ILogonActivity logonActivity;
        private readonly AppSettings appSettings;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="configurationProvider">configurationProvider DI.</param>
        /// <param name="telemetryClient">The logging mechanism through Application Insights.</param>
        /// <param name="graphUtility">Having the ability to DI the mechanism to get the token from MS Graph.</param>
        /// <param name="logonActivity">Logon activity.</param>
        /// <param name="appSettings">app settings.</param>
        /// <param name="teamDepartmentMappingProvider">configurationb Provider DI.</param>
        public HomeController(
            BusinessLogic.Providers.IConfigurationProvider configurationProvider,
            TelemetryClient telemetryClient,
            IGraphUtility graphUtility,
            ILogonActivity logonActivity,
            AppSettings appSettings,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider)
        {
            this.configurationProvider = configurationProvider;
            this.telemetryClient = telemetryClient;
            this.graphUtility = graphUtility;
            this.logonActivity = logonActivity;
            this.appSettings = appSettings;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
        }

        /// <summary>
        /// The landing page.
        /// </summary>
        /// <returns>The default landing page.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            this.ViewBag.SubmitMessage = this.ViewBag.SubmitMessage ?? string.Empty;
            if (this.User.Identity.IsAuthenticated)
            {
                this.ViewBag.EmailId = this.User.Identity.Name;
                this.ViewBag.TenantId = this.User.GetTenantId();
                this.ViewBag.IsUserLoggedIn = true;
            }
            else
            {
                this.ViewBag.IsUserLoggedIn = false;
            }

            return this.View();
        }

        /// <summary>
        /// Parse the TeamID coming in first, and then proceed to save it.
        /// </summary>
        /// <param name="dataToSave">The data to save.</param>
        /// <returns>Having the action result returned.</returns>
        [HttpPost]
        public async Task<IActionResult> ParseAndSaveDataAsync(HomeViewModel dataToSave)
        {
            if (dataToSave != null && dataToSave.WfmApiEndpoint != null &&
                dataToSave.WfmSuperUsername != null &&
                dataToSave.WfmSuperUserPassword != null)
            {
                ConfigurationEntity newConfiguration = new ConfigurationEntity();

                // condition that executes in case of Submit
                if (string.IsNullOrWhiteSpace(dataToSave.ConfigurationId))
                {
                    newConfiguration = this.CreateNewConfigurationAsync(dataToSave);
                }

                // condition that executes in case of Update
                else if (!string.IsNullOrWhiteSpace(dataToSave.ConfigurationId))
                {
                    // getting already existing configuration details
                    newConfiguration = await this.configurationProvider.GetConfigurationAsync(dataToSave.TenantId, dataToSave.ConfigurationId).ConfigureAwait(false);

                    // updating the changed field
                    newConfiguration.WfmApiEndpoint = dataToSave.WfmApiEndpoint;
                }

                if (newConfiguration != null)
                {
                    var saveConfigurationProps = new Dictionary<string, string>()
                    {
                        { "TenantId", dataToSave.TenantId },
                        { "WorkforceProvider", dataToSave.WfmProviderName },
                        { "WorkforceSuperUserName", dataToSave.WfmSuperUsername },
                        { "WorkforceSuperUserPassword", dataToSave.WfmSuperUserPassword },
                        { "WorkforceAPIEndpoint", dataToSave.WfmApiEndpoint },
                    };

                    this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, saveConfigurationProps);

                    var loginKronos = await this.logonActivity.LogonAsync(
                        dataToSave.WfmSuperUsername,
                        dataToSave.WfmSuperUserPassword,
                        new Uri(dataToSave.WfmApiEndpoint)).ConfigureAwait(false);

                    // When given Kronos URL is incorrect. loginKronos will be null.
                    if (loginKronos == null)
                    {
                        return this.RedirectToAction("Index").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.InvalidKronosURL);
                    }
                    else
                    if (loginKronos.Status == ApiConstants.Success)
                    {
                        // executes for both save and update operations
                        await this.configurationProvider.SaveOrUpdateConfigurationAsync(newConfiguration).ConfigureAwait(false);

                        var userDetailsDict = new Dictionary<string, string>()
                            {
                                { "WorkforceSuperUserName", dataToSave.WfmSuperUsername },
                                { "WorkforceSuperUserPassword", dataToSave.WfmSuperUserPassword },
                            };

                        // save or update the user credentials in keyvault
                        this.SaveSecrets(userDetailsDict);

                        // setting appsettings properties WorkforceSuperUserName & WorkforceSuperUserPassword to their updated values
                        this.appSettings.WfmSuperUsername = dataToSave.WfmSuperUsername;
                        this.appSettings.WfmSuperUserPassword = dataToSave.WfmSuperUserPassword;

                        // condition to update the details if already exists
                        if (!string.IsNullOrWhiteSpace(dataToSave.ConfigurationId))
                        {
                            return this.RedirectToAction("Index").WithSuccess(Resources.UpdateNotificationHeaderText, string.Empty);
                        }

                        return this.RedirectToAction("Index").WithSuccess(Resources.SuccessNotificationHeaderText, Resources.ConfigurationSavedSuccessNextStepsText);
                    }

                    // When Kronos login is failed due to incorrect credentials.
                    else
                    {
                        return this.RedirectToAction("Index").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.KronosErrorContentText);
                    }
                }
                else
                {
                    return this.RedirectToAction("Index").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.UnableToFetchConfiguration);
                }
            }
            else
            {
                return this.RedirectToAction("Index").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.MandatoryCredentialsNotProvided);
            }
        }

        /// <summary>
        /// Having the necessary signinoidc page.
        /// </summary>
        /// <returns>A blank page with content of blah.</returns>
        [Route("/signin-oidc")]
        public ActionResult SignInOidc()
        {
            return this.Content(string.Empty);
        }

        /// <summary>
        /// The method will get the necessary configurations that have been saved.
        /// </summary>
        /// <returns>A list of the configurations.</returns>
        [HttpGet]
        public async Task<ActionResult> GetConfigurationsAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name} to get all the configurations");
            List<ConfigurationEntity> configurations = await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false);
            ConfigEntityViewModel config = new ConfigEntityViewModel();

            // Setting the username and password from keyvault
            if (configurations != null && configurations[0] != null)
            {
                config.TenantId = configurations[0].TenantId;
                config.ConfigurationId = configurations[0].ConfigurationId;
                config.WfmSuperUsername = this.appSettings.WfmSuperUsername;
                config.WfmSuperUserPassword = this.appSettings.WfmSuperUserPassword;
                config.WfmApiEndpoint = configurations[0].WfmApiEndpoint;
            }

            if (configurations.Count > 0)
            {
                return this.PartialView("_DataTable", config);
            }
            else
            {
                return this.PartialView("_DataTable", null);
            }
        }

        /// <summary>
        /// Method to register workforce integrations.
        /// </summary>
        /// <param name="tenantId">The Tenant ID.</param>
        /// <param name="configurationId">The configuration ID of the selected configuration.</param>
        /// <returns>A unit of execution.</returns>
        public async Task<IActionResult> RegisterWorkforceIntegrationAsync(
            [FromQuery] string tenantId,
            string configurationId)
        {
            var workforceIntegrationRegProps = new Dictionary<string, string>()
            {
                { "TenantId", tenantId },
                { "ConfigurationId", configurationId },
            };

            // Getting the configuration entity here.
            var configurationEntity = await this.configurationProvider.GetConfigurationAsync(tenantId, configurationId).ConfigureAwait(false);
            if (configurationEntity != null)
            {
                string wfiDisplayName = $"{configurationEntity.WfmProvider}ShiftsIntegration";

                if (!string.IsNullOrEmpty(configurationEntity.WorkforceIntegrationId))
                {
                    return this.RedirectToAction("Index", "Home").WithSuccess(Resources.WFIGeneralHeaderText, string.Format(CultureInfo.InvariantCulture, Resources.WFIAlreadyRegHeaderText, configurationEntity.WorkforceIntegrationId));
                }

                // Having the workforceIntegration request object created
                var workforceIntegrationRequest = this.CreateWorkforceIntegrationRequest(wfiDisplayName);
                workforceIntegrationRegProps.Add("workforceIntegrationDisplayName", wfiDisplayName);

                var clientId = this.appSettings.ClientId;
                var instance = this.appSettings.Instance;
                var clientSecret = this.appSettings.ClientSecret;

                var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var graphAccessToken = await this.graphUtility.GetAccessTokenAsync(
                    tenantId,
                    instance,
                    clientId,
                    clientSecret,
                    userId).ConfigureAwait(false);

                configurationEntity.WorkforceIntegrationSecret = workforceIntegrationRequest.Encryption.Secret;
                configurationEntity.AdminAadObjectId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                await this.configurationProvider.SaveOrUpdateConfigurationAsync(configurationEntity).ConfigureAwait(false);

                // Call the Graph API to register the workforce integration
                var workforceIntegrationResponse = await this.graphUtility.RegisterWorkforceIntegrationAsync(
                    workforceIntegrationRequest,
                    graphAccessToken).ConfigureAwait(false);

                if (workforceIntegrationResponse.IsSuccessStatusCode)
                {
                    // Making sure to have the workforce integration properly saved in the Azure Table storage.
                    var workforceResponse = await workforceIntegrationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var workforceIntegrationResponseObj = JsonConvert.DeserializeObject<WorkforceIntegrationEntity>(workforceResponse);
                    configurationEntity.WorkforceIntegrationId = workforceIntegrationResponseObj.Id;
                    configurationEntity.WorkforceIntegrationSecret = workforceIntegrationRequest.Encryption.Secret;
                    configurationEntity.AdminAadObjectId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                    await this.configurationProvider.SaveOrUpdateConfigurationAsync(configurationEntity).ConfigureAwait(false);

                    return this.RedirectToAction("Index", "Home").WithSuccess(Resources.WFIRegSuccessHeaderText, string.Format(CultureInfo.InvariantCulture, Resources.WFIRegSuccessContentText, workforceIntegrationResponseObj.DisplayName));
                }
                else
                {
                    var failedResponseContent = await workforceIntegrationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                    workforceIntegrationRegProps.Add("FailedResponse", failedResponseContent);
                    return this.RedirectToAction("Index", "Home").WithErrorMessage(Resources.WFIRegFailedHeaderText, string.Format(CultureInfo.InvariantCulture, Resources.WFIRegFailedContentText, wfiDisplayName));
                }
            }

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, workforceIntegrationRegProps);

            return this.RedirectToAction("Index");
        }

        /// <summary>
        /// This method would delete the Workforce Integration entity and should update the respective entities accordingly.
        /// </summary>
        /// <param name="tenantId">The Tenant ID.</param>
        /// <param name="configurationId">The configuration ID.</param>
        /// <returns>Returns the view accordingly.</returns>
        public async Task<IActionResult> DeleteWorkforceIntegrationAsync(
            [FromQuery] string tenantId,
            string configurationId)
        {
            var deleteWFIProps = new Dictionary<string, string>()
            {
                { "TenantId", tenantId },
                { "ConfigurationId", configurationId },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, deleteWFIProps);

            var configurationEntity = await this.configurationProvider.GetConfigurationAsync(tenantId, configurationId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(configurationEntity?.WorkforceIntegrationId))
            {
                var teamDeptMappingEntity = await this.teamDepartmentMappingProvider.GetMappedTeamToDeptsWithJobPathsAsync().ConfigureAwait(false);
                if (teamDeptMappingEntity != null && teamDeptMappingEntity.Count > 0)
                {
                    return this.RedirectToAction("Index", "Home").WithErrorMessage(Resources.WFIGeneralHeaderText, string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.WFIDeleteFailedDueToTeamDeptMappingText,
                        configurationEntity?.WorkforceIntegrationId));
                }

                var clientId = this.appSettings.ClientId;
                var instance = this.appSettings.Instance;
                var clientSecret = this.appSettings.ClientSecret;
                var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                var graphAccessToken = await this.graphUtility.GetAccessTokenAsync(tenantId, instance, clientId, clientSecret, userId).ConfigureAwait(false);

                var wfiDeletionResponse = await this.graphUtility.DeleteWorkforceIntegrationAsync(
                    configurationEntity?.WorkforceIntegrationId,
                    graphAccessToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(wfiDeletionResponse))
                {
                    var prevWorkforceIntegrationId = configurationEntity?.WorkforceIntegrationId;
                    configurationEntity.WorkforceIntegrationId = string.Empty;
                    await this.configurationProvider.DeleteConfigurationAsync(configurationEntity).ConfigureAwait(false);
                    return this.RedirectToAction("Index", "Home").WithSuccess(Resources.WFIGeneralHeaderText, string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.WfiDeletionSuccessText,
                        prevWorkforceIntegrationId));
                }
                else
                {
                    return this.RedirectToAction("Index", "Home").WithErrorMessage(Resources.WFIGeneralHeaderText, string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.WfiDeletionFailed,
                        configurationEntity?.WorkforceIntegrationId));
                }
            }
            else if (configurationEntity != null)
            {
                await this.configurationProvider.DeleteConfigurationAsync(configurationEntity).ConfigureAwait(false);
                return this.RedirectToAction("Index", "Home").WithSuccess(Resources.SuccessNotificationHeaderText, Resources.ConfigurationEntityDeletionSuccessContent);
            }
            else
            {
                return this.RedirectToAction("Index", "Home").WithErrorMessage(Resources.WFIGeneralHeaderText, string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.WfiDeletionFailed,
                    configurationEntity?.WorkforceIntegrationId));
            }
        }

        /// <summary>
        /// This method will be handling the click of the next button.
        /// </summary>
        /// <returns>An ActionResult.</returns>
        public IActionResult GoToNext()
        {
            return this.RedirectToAction("Index", "UserMapping");
        }

        /// <summary>
        /// Returns a new <see cref="ConfigurationEntity"/> to save into Azure table storage.
        /// </summary>
        /// <param name="viewModel">The view model containing the attributes.</param>
        /// <returns>A <see cref="ConfigurationEntity"/> to save in Azure table storage.</returns>
        private ConfigurationEntity CreateNewConfigurationAsync(HomeViewModel viewModel)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            ConfigurationEntity configuration = new ConfigurationEntity
            {
                ConfigurationId = Guid.NewGuid().ToString(),
                TenantId = viewModel.TenantId,
                WfmProvider = viewModel.WfmProviderName,
                WfmApiEndpoint = viewModel.WfmApiEndpoint,
            };

            return configuration;
        }

        /// <summary>
        /// Method creates the workforceIntegration request object to pass to Graph.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <returns>An object of type <see cref="WorkforceIntegration"/>.</returns>
        private WorkforceIntegration CreateWorkforceIntegrationRequest(string displayName)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            WorkforceIntegration wfIntegration = new WorkforceIntegration()
            {
                DisplayName = displayName,
                ApiVersion = 1,
                IsActive = true,
                Url = this.appSettings.IntegrationApiUrl,
                SupportedEntities = Constants.WFISupports,
                Encryption = this.EstablishEncryption(),
            };

            return wfIntegration;
        }

        /// <summary>
        /// Method that will be able to establish the encryption properties.
        /// </summary>
        /// <returns>Returns an object of type <see cref="Encryption"/> which contains a randomly generated key.</returns>
        private Encryption EstablishEncryption()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            Encryption wfEncryption = new Encryption()
            {
                Protocol = "sharedSecret",
                Secret = this.GenerateSecret(),
            };

            return wfEncryption;
        }

        /// <summary>
        /// Generates the symmetric key for Graph API.
        /// </summary>
        /// <returns>A string that represents the key.</returns>
        private string GenerateSecret()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            var key1 = Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            var key2 = Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.InvariantCulture);
            return key1 + key2;
        }

        /// <summary>
        /// this method can be used to store secrets to key vault.
        /// </summary>
        /// <param name="configurationDetails">The dictionary of key value pairs.</param>
        private void SaveSecrets(Dictionary<string, string> configurationDetails)
        {
            foreach (KeyValuePair<string, string> keyValuePair in configurationDetails)
            {
                bool isSaved = this.appSettings.SetConfigToKeyVault(keyValuePair.Key, keyValuePair.Value);
                if (isSaved)
                {
                    this.telemetryClient.TrackTrace("Settings saved" + keyValuePair.Key);
                }
                else
                {
                    this.telemetryClient.TrackTrace("Settings failed to save" + keyValuePair.Key);
                }
            }
        }
    }
}