// <copyright file="SyncKronosToShiftsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
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
        private readonly UserController usersController;
        private readonly OpenShiftController openShiftController;
        private readonly OpenShiftRequestController openShiftRequestController;
        private readonly SwapShiftController swapShiftController;
        private readonly TimeOffController timeOffController;
        private readonly TimeOffReasonController timeOffReasonController;
        private readonly ShiftController shiftController;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncKronosToShiftsController"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="configurationProvider">The Configuration Provider DI.</param>
        /// <param name="usersController">UsersController DI.</param>
        /// <param name="openShiftController">OpenShiftController DI.</param>
        /// <param name="openShiftRequestController">OpenShiftRequestController DI.</param>
        /// <param name="swapShiftController">SwapShiftController DI.</param>
        /// <param name="timeOffController">TimeOffController DI.</param>
        /// <param name="timeOffReasonController">TimeOffReasonController DI.</param>
        /// <param name="shiftController">ShiftController DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public SyncKronosToShiftsController(
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider,
            UserController usersController,
            OpenShiftController openShiftController,
            OpenShiftRequestController openShiftRequestController,
            SwapShiftController swapShiftController,
            TimeOffController timeOffController,
            TimeOffReasonController timeOffReasonController,
            ShiftController shiftController,
            BackgroundTaskWrapper taskWrapper)
        {
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
            this.usersController = usersController;
            this.openShiftController = openShiftController;
            this.openShiftRequestController = openShiftRequestController;
            this.swapShiftController = swapShiftController;
            this.timeOffController = timeOffController;
            this.timeOffReasonController = timeOffReasonController;
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

            if (bool.TryParse(isRequestFromLogicApp, out bool isFromLogicApp))
            {
                this.taskWrapper.Enqueue(this.ProcessKronosToShiftsShiftsAsync(isRequestFromLogicApp));

                this.telemetryClient.TrackTrace($"SyncKronosToShifts ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
                return this.Ok(string.Empty);
            }
            else
            {
                this.telemetryClient.TrackTrace($"Unable to call SyncKronosToShifts service at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}. {Resource.IncorrectArgumentType}", telemetryProps);
                return this.BadRequest(Resource.IncorrectArgumentType);
            }
        }

        /// <summary>
        /// Process all the entities from Kronos to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">true if the call is coming from logic app, false otherwise.</param>
        /// <returns>Returns task.</returns>
        private async Task ProcessKronosToShiftsShiftsAsync(string isRequestFromLogicApp)
        {
            // We require a successful user sync to ensure we are only processing active employees
            if (!await this.ProcessTask(this.usersController.ProcessUsersAsync(), Resource.ProcessUsersAsync).ConfigureAwait(false))
            {
                this.telemetryClient.TrackTrace($"{Resource.ProcessUsersAsync} failed. No other syncs were performed. ");
                return;
            }

            await this.ProcessTask(this.openShiftController.ProcessOpenShiftsAsync(isRequestFromLogicApp), Resource.ProcessOpenShiftsAsync).ConfigureAwait(false);
            bool isOpenShiftRequestSyncSuccessful = await this.ProcessTask(this.openShiftRequestController.ProcessOpenShiftsRequests(isRequestFromLogicApp), Resource.ProcessOpenShiftsRequests).ConfigureAwait(false);
            bool isSwapShiftRequestSyncSuccessful = await this.ProcessTask(this.swapShiftController.ProcessSwapShiftsAsync(isRequestFromLogicApp), Resource.ProcessSwapShiftsAsync).ConfigureAwait(false);
            bool isMapPayCodeTimeOffReasonsSuccessful = await this.ProcessTask(this.timeOffReasonController.MapPayCodeTimeOffReasonsAsync(isRequestFromLogicApp), Resource.MapPayCodeTimeOffReasonsAsync).ConfigureAwait(false);

            // Sync TimeOff only if Paycodes in Kronos synced successfully.
            if (isMapPayCodeTimeOffReasonsSuccessful)
            {
                await this.ProcessTask(this.timeOffController.ProcessTimeOffsAsync(isRequestFromLogicApp), Resource.ProcessTimeOffsAsync).ConfigureAwait(false);
            }
            else
            {
                this.telemetryClient.TrackTrace($"{Resource.MapPayCodeTimeOffReasonsAsync} status: " + isMapPayCodeTimeOffReasonsSuccessful);
            }

            // sync Shifts from Kronos to Shifts only if OpenShiftRequest and SwapShiftRequest sync is successful.
            if (isSwapShiftRequestSyncSuccessful && isOpenShiftRequestSyncSuccessful)
            {
                await this.ProcessTask(this.shiftController.ProcessShiftsAsync(isRequestFromLogicApp), Resource.ProcessShiftsAsync).ConfigureAwait(false);
            }
            else
            {
                this.telemetryClient.TrackTrace("Shifts sync not processed. isSwapShiftRequestSuccessful: " + isSwapShiftRequestSyncSuccessful + ", isOpenShiftRequestSuccessful: " + isOpenShiftRequestSyncSuccessful);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessKronosToShiftsShiftsAsync} completed at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Keeping consistent with previous code.")]
        private async Task<bool> ProcessTask(Task task, string name)
        {
            this.telemetryClient.TrackTrace($"Begun {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"Failed {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
                return false;
            }

            this.telemetryClient.TrackTrace($"Ended {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return true;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Keeping consistent with previous code.")]
        private async Task<bool> ProcessTask(Task<bool> task, string name)
        {
            var result = false;
            this.telemetryClient.TrackTrace($"Begun {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            try
            {
                result = await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                this.telemetryClient.TrackTrace($"Failed {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
                return result;
            }

            this.telemetryClient.TrackTrace($"Ended {name} at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return result;
        }
    }
}