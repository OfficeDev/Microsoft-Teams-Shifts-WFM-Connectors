// <copyright file="SyncKronosToShiftsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;

    /// <summary>
    /// Sync Kronos To Shifts Controller.
    /// </summary>
    [Route("api/SyncKronosToShifts")]
    public class SyncKronosToShiftsController : ControllerBase
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfigurationProvider configurationProvider;
        private readonly OpenShiftController openShiftController;
        private readonly OpenShiftRequestController openShiftRequestController;
        private readonly SwapShiftController swapShiftController;
        private readonly TimeOffController timeOffController;
        private readonly TimeOffReasonController timeOffReasonController;
        private readonly TimeOffRequestsController timeOffRequestsController;
        private readonly ShiftController shiftController;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncKronosToShiftsController"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="configurationProvider">The Configuration Provider DI.</param>
        /// <param name="openShiftController">OpenShiftController DI.</param>
        /// <param name="openShiftRequestController">OpenShiftRequestController DI.</param>
        /// <param name="swapShiftController">SwapShiftController DI.</param>
        /// <param name="timeOffController">TimeOffController DI.</param>
        /// <param name="timeOffReasonController">TimeOffReasonController DI.</param>
        /// <param name="timeOffRequestsController">TimeOffRequestsController DI.</param>
        /// <param name="shiftController">ShiftController DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public SyncKronosToShiftsController(
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider,
            OpenShiftController openShiftController,
            OpenShiftRequestController openShiftRequestController,
            SwapShiftController swapShiftController,
            TimeOffController timeOffController,
            TimeOffReasonController timeOffReasonController,
            TimeOffRequestsController timeOffRequestsController,
            ShiftController shiftController,
            BackgroundTaskWrapper taskWrapper)
        {
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
            this.openShiftController = openShiftController;
            this.openShiftRequestController = openShiftRequestController;
            this.swapShiftController = swapShiftController;
            this.timeOffController = timeOffController;
            this.timeOffReasonController = timeOffReasonController;
            this.timeOffRequestsController = timeOffRequestsController;
            this.shiftController = shiftController;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Http get method to start Kronos to Shifts sync.
        /// </summary>
        /// <param name="isRequestFromLogicApp">True if request is coming from logic app, false otherwise.</param>
        /// <returns>Returns Ok if request is successfully queued.</returns>
        [Authorize(Policy = "AppID")]
        [HttpGet]
        [Route("StartSync/{isRequestFromLogicApp}")]
        public ActionResult SyncKronosToShifts(string isRequestFromLogicApp)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "APIRoute", this.ControllerContext.ActionDescriptor.AttributeRouteInfo.Name },
            };

            this.telemetryClient.TrackTrace($"SyncKronosToShifts starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            this.taskWrapper.Enqueue(this.ProcessKronosToShiftsShiftsAsync(isRequestFromLogicApp));

            using (StringContent stringContent = new StringContent(string.Empty))
            {
                this.telemetryClient.TrackTrace($"SyncKronosToShifts ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
                return this.Ok(stringContent);
            }
        }

        /// <summary>
        /// Process all the entities from Kronos to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">true if the call is coming from logic app, false otherwise.</param>
        /// <returns>Returns task.</returns>
#pragma warning disable CA1031 // Do not catch general exception types - Suppressing as there may be unknown points of failure.
        private async Task ProcessKronosToShiftsShiftsAsync(string isRequestFromLogicApp)
        {
            var isOpenShiftRequestSyncSuccessful = false;
            var isSwapShiftRequestSyncSuccessful = false;
            var isMapPayCodeTimeOffReasonsSuccessful = false;

            try
            {
                this.telemetryClient.TrackTrace($"{Resource.ProcessKronosToShiftsShiftsAsync} start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);

                // Sync open shifts from Kronos to Shifts.
                await this.openShiftController.ProcessOpenShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);
                this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"An error has happened when syncing the open shifts: {ex.Message}", SeverityLevel.Error);
            }

            try
            {
                // Sync open shifts requests from Kronos to Shifts.
                await this.openShiftRequestController.ProcessOpenShiftsRequests(isRequestFromLogicApp).ConfigureAwait(false);
                isOpenShiftRequestSyncSuccessful = true;
                this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
            }
            catch (Exception ex)
            {
                isOpenShiftRequestSyncSuccessful = false;
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"An error has happened when syncing the open shift requests: {ex.Message}", SeverityLevel.Error);
            }

            try
            {
                // Sync swap shifts from Kronos to Shifts.
                await this.swapShiftController.ProcessSwapShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);
                isSwapShiftRequestSyncSuccessful = true;
                this.telemetryClient.TrackTrace($"{Resource.ProcessSwapShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
            }
            catch (Exception ex)
            {
                isSwapShiftRequestSyncSuccessful = false;
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"An error has happened when syncing the swap shift requests: {ex.Message}", SeverityLevel.Error);
            }

            try
            {
                // Sync timeoffreasons from Kronos to Shifts.
                await this.timeOffReasonController.MapPayCodeTimeOffReasonsAsync().ConfigureAwait(false);
                isMapPayCodeTimeOffReasonsSuccessful = true;
                this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
            }
            catch (Exception ex)
            {
                isMapPayCodeTimeOffReasonsSuccessful = false;
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"An error has happened when syncing the timeoff reasons: {ex.Message}", SeverityLevel.Error);
            }

            // Sync timeoff and timeoff requests only if paycodes in Kronos synced successfully.
            if (isMapPayCodeTimeOffReasonsSuccessful)
            {
                try
                {
                    // Sync timeoff from Kronos to Shifts.
                    await this.timeOffController.ProcessTimeOffsAsync(isRequestFromLogicApp).ConfigureAwait(false);
                    this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(ex);
                    this.telemetryClient.TrackTrace($"An error has happened when syncing the timeoffs: {ex.Message}", SeverityLevel.Error);
                }

                try
                {
                    // Sync timeoff requests from Kronos to Shifts.
                    await this.timeOffRequestsController.ProcessTimeOffRequestsAsync(isRequestFromLogicApp).ConfigureAwait(false);
                    this.telemetryClient.TrackTrace($"{Resource.SyncTimeOffRequestsFromShiftsToKronos} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(ex);
                    this.telemetryClient.TrackTrace($"An error has happened when syncing the timeoff requests: {ex.Message}", SeverityLevel.Error);
                }
            }
            else
            {
                this.telemetryClient.TrackTrace($"Skipping the sync of both TimeOff and TimeOff Request entities as there may have been an error with the Kronos paycode and Shifts timeoff reason mapping. Status:  {isMapPayCodeTimeOffReasonsSuccessful}");
            }

            // Sync shifts from Kronos to Shifts only if open shift request and swap shift request sync is successful.
            if (isSwapShiftRequestSyncSuccessful && isOpenShiftRequestSyncSuccessful)
            {
                // Sync shifts from Kronos to Shifts.
                await this.shiftController.ProcessShiftsAsync(isRequestFromLogicApp).ConfigureAwait(false);
                this.telemetryClient.TrackTrace($"{Resource.ProcessShiftsAsync} completed from {Resource.ProcessKronosToShiftsShiftsAsync} ");
            }

            // Do not sync shifts from Kronos to Shifts. Log the status of open shift request and swap shift request sync.
            else
            {
                this.telemetryClient.TrackTrace("Shifts sync not processed. isSwapShiftRequestSuccessful: " + isSwapShiftRequestSyncSuccessful + ", isOpenShiftRequestSuccessful: " + isOpenShiftRequestSyncSuccessful);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessKronosToShiftsShiftsAsync} completed at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);
        }
#pragma warning restore CA1031 // Do not catch general exception types - Suppressing as there may be unknown points of failure.
    }
}