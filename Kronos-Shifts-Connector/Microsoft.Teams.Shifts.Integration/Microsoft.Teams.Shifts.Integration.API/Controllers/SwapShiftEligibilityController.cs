// <copyright file="SwapShiftEligibilityController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShiftEligibility;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using static Microsoft.AspNetCore.Http.StatusCodes;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using static Microsoft.Teams.Shifts.Integration.API.Common.ResponseHelper;
    using SwapResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShiftEligibility.Response;

    /// <summary>
    /// This is the SwapShiftEligibilityController.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/[controller]")]
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

        /// <summary>
        /// Gets a list of eligible shifts to be swapped with.
        /// </summary>
        /// <param name="shiftId">The id of the shift that wants to be swapped.</param>
        /// <param name="kronosTimeZone">The time zone of the for the user swapping the shift.</param>
        /// <returns>A <see cref="Task{ShiftsIntegResponse}"/> representing the result of the asynchronous operation.</returns>
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

            if (startDate < DateTime.Now || endDate < DateTime.Now)
            {
                return CreateResponse(null, Status404NotFound, "You can't swap a shift that has already started.");
            }

            var offeredStartTime = startDate.TimeOfDay.ToString();
            var offeredEndTime = endDate.TimeOfDay.ToString();
            var offeredShiftDate = this.utility.ConvertToKronosDate(startDate);
            var swapShiftDate = this.utility.ConvertToKronosDate(endDate);
            var days = this.GetDateList();
            List<TeamsShiftMappingEntity> eligibleShifts = new List<TeamsShiftMappingEntity>();

            foreach (var day in days)
            {
                var kronosDate = this.utility.ConvertToKronosDate(day);
                var eligibleEmployees = await this.swapShiftEligibilityActivity.SendEligibilityRequestAsync(
                    new Uri(configuration.WfmEndPoint),
                    configuration.KronosSession,
                    offeredStartTime,
                    offeredEndTime,
                    offeredShiftDate,
                    kronosDate,
                    shift.KronosPersonNumber)
                        .ConfigureAwait(false);

                if (eligibleEmployees?.Status != Success)
                {
                    return CreateResponse(null, Status404NotFound, $"There was an error finding employees eligible to take this shift on {day.ToShortDateString()}.");
                }

                var userLists = eligibleEmployees.Person.Select(x => new UserDetailsModel { KronosPersonNumber = x.PersonNumber });

                var monthPartition = Utility.GetMonthPartition(kronosDate, kronosDate)[0];

                eligibleShifts.AddRange(await this.shiftMappingEntityProvider.GetAllShiftMappingEntitiesInBatchAsync(
                    userLists,
                    monthPartition,
                    kronosDate,
                    kronosDate).ConfigureAwait(false));
            }

            return CreateResponse(
                shift.RowKey,
                Status200OK,
                eligibleShifts
                    .Where(x => x.ShiftStartDate > DateTime.Now)
                    .Select(x => x.RowKey));
        }

        private List<DateTime> GetDateList()
        {
            List<DateTime> dates = new List<DateTime>();
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(int.Parse(this.appSettings.FutureSwapEligibilityDays));

            for (DateTime i = startDate; i <= endDate; i = i.AddDays(1))
            {
                dates.Add(i);
            }

            return dates;
        }
    }
}