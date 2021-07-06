// <copyright file="SwapShiftEligibilityController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShiftEligibility;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using static Microsoft.AspNetCore.Http.StatusCodes;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;

    /// <summary>
    /// This is the SwapShiftController.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/[controller]")]
    [ApiController]
    public class SwapShiftEligibilityController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly ISwapShiftEligibilityActivity swapShiftEligibilityActivity;
        private readonly TelemetryClient telemetryClient;
        private readonly Utility utility;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapShiftEligibilityController"/> class.
        /// </summary>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="swapShiftEligibilityActivity">Swap Shift Eligibility activity.</param>
        /// <param name="utility">UniqueId Utility DI.</param>
        /// <param name="httpClientFactory">The HTTP Client DI.</param>
        /// <param name="shiftMappingEntityProvider">Shift mapping entity provider DI.</param>
        public SwapShiftEligibilityController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            ISwapShiftEligibilityActivity swapShiftEligibilityActivity,
            Utility utility,
            IHttpClientFactory httpClientFactory,
            IShiftMappingEntityProvider shiftMappingEntityProvider)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.telemetryClient = telemetryClient;
            this.swapShiftEligibilityActivity = swapShiftEligibilityActivity;
            this.utility = utility;
            this.httpClientFactory = httpClientFactory;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
        }

        public async Task<ShiftsIntegResponse> GetEligibleShiftsForSwappingAsync(string shiftId, string kronosTimeZone)
        {
            var configuration = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            if (configuration?.IsAllSetUpExists == false)
            {
                return CreateResponse(null, Status404NotFound, "The app is not configured correctly.");
            }

            var shift = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(shiftId).ConfigureAwait(false);
            var startDate = this.utility.UTCToKronosTimeZone(shift.ShiftStartDate, kronosTimeZone);
            var endDate = this.utility.UTCToKronosTimeZone(shift.ShiftEndDate, kronosTimeZone);
            var offeredStartTime = startDate.TimeOfDay.ToString();
            var offeredEndTime = endDate.TimeOfDay.ToString();
            var offeredShiftDate = this.ConvertToKronosDate(startDate);
            var swapShiftDate = this.ConvertToKronosDate(endDate);
            var employeeNumber = shift.KronosPersonNumber;

            var eligibleEmployees = await this.swapShiftEligibilityActivity.SendEligibilityRequestAsync(
                new Uri(configuration.WfmEndPoint), configuration.KronosSession, offeredStartTime, offeredEndTime, offeredShiftDate, swapShiftDate, employeeNumber).ConfigureAwait(false);

            if (eligibleEmployees?.Status != Success)
            {
                return CreateResponse(null, Status404NotFound, "There are no employees eligible to take this shift.");
            }

            var monthPartition = Utility.GetMonthPartition(swapShiftDate, swapShiftDate)[0];
            var users = new List<UserDetailsModel>();
            foreach (var p in eligibleEmployees.Person)
            {
                users.Add(new UserDetailsModel {KronosPersonNumber = p.PersonNumber});
            }

            var list = await this.shiftMappingEntityProvider.GetAllShiftMappingEntitiesInBatchAsync(users, monthPartition, swapShiftDate, swapShiftDate).ConfigureAwait(false);
            return CreateResponse(null, Status200OK, "Successfully added eligible shifts.");
        }

        private static ShiftsIntegResponse CreateResponse(string id, int statusCode, string error = null)
        {
            return new ShiftsIntegResponse
            {
                Id = id,
                Status = statusCode,
                Body = new Body
                {
                    Error = new ResponseError { Message = error },
                    ETag = null,
                },
            };
        }

        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "This format is needed for kronos calls.")]
        private string ConvertToKronosDate(DateTime date) => date.ToString(this.appSettings.KronosQueryDateSpanFormat);
    }
}