// <copyright file="UsersController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;

    /// <summary>
    /// The Users controller.
    /// </summary>
    [Route("api/Users")]
    [Authorize(Policy = "AppID")]
    public class UserController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IHyperFindActivity hyperFindActivity;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly Utility utility;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="appSettings">Application Settings DI.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="hyperFindActivity">Kronos Hyper Find Activity DI.</param>
        /// <param name="userMappingProvider">The User Mapping Provider DI.</param>
        /// <param name="utility">Utility DI.</param>
        public UserController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IUserMappingProvider userMappingProvider,
            IHyperFindActivity hyperFindActivity,
            Utility utility)
        {
            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.hyperFindActivity = hyperFindActivity;
            this.userMappingProvider = userMappingProvider;
            this.utility = utility;
        }

        /// <summary>
        /// Start user sync from Kronos to Shifts.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task<bool> ProcessUsersAsync()
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessUsersAsync} started.");

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            var kronosUserDetailsQuery = this.appSettings.KronosUserDetailsQuery;

            // This will retrieve all active employees for using the 'All Home' query
            var response = await this.hyperFindActivity.GetHyperFindQueryValuesAsync(
                new Uri(allRequiredConfigurations.WfmEndPoint),
                allRequiredConfigurations.KronosSession,
                DateTime.Now.ToString("M/dd/yyyy", CultureInfo.InvariantCulture),
                DateTime.Now.ToString("M/dd/yyyy", CultureInfo.InvariantCulture),
                kronosUserDetailsQuery,
                ApiConstants.PublicVisibilityCode).ConfigureAwait(false);

            if (response.Status == ApiConstants.Failure)
            {
                this.telemetryClient.TrackTrace($"Unable to retrieve the list of active users in Kronos with error: {response.Error?.ErrorCode}");
                return false;
            }

            // Get the mapped user details from user to user mapping table.
            var mappedUsersResult = await this.userMappingProvider.GetAllMappedUserDetailsAsync().ConfigureAwait(false);

            foreach (var mappedUser in mappedUsersResult)
            {
                var isUserActive = response.HyperFindResult.Any(h => h.PersonNumber == mappedUser.RowKey);

                if (!isUserActive && mappedUser.IsActive)
                {
                    // User is either inactive or terminated in Kronos but appears as active in cache.
                    mappedUser.IsActive = false;
                    await this.userMappingProvider.SaveOrUpdateUserMappingEntityAsync(mappedUser).ConfigureAwait(false);
                }

                if (isUserActive && !mappedUser.IsActive)
                {
                    // User is active in Kronos but has previosuly been marked as inactive in cache.
                    mappedUser.IsActive = true;
                    await this.userMappingProvider.SaveOrUpdateUserMappingEntityAsync(mappedUser).ConfigureAwait(false);
                }
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessUsersAsync} finished.");
            return true;
        }
    }
}