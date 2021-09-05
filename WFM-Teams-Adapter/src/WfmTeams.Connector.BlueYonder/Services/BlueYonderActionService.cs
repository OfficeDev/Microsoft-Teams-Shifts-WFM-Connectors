// ---------------------------------------------------------------------------
// <copyright file="BlueYonderActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using WfmTeams.Adapter;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Exceptions;
    using WfmTeams.Connector.BlueYonder.Extensions;
    using WfmTeams.Connector.BlueYonder.Mappings;
    using WfmTeams.Connector.BlueYonder.Models;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderActionService : BlueYonderBaseService, IWfmActionService
    {
        private readonly IAvailabilityMap _availabilityMap;
        private readonly ICacheService _cacheService;

        public BlueYonderActionService(BlueYonderPersonaOptions byOptions, ISecretsService secretsService, IBlueYonderClientFactory clientFactory, IStringLocalizer<BlueYonderConfigService> stringLocalizer, IAvailabilityMap availabilityMap, ICacheService cacheService)
            : base(byOptions, secretsService, clientFactory, stringLocalizer)
        {
            _availabilityMap = availabilityMap ?? throw new ArgumentNullException(nameof(availabilityMap));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        #region Shift Swap Requests

        public async Task<WfmResponse> CreateShiftSwapRequestAsync(WfmShiftSwapModel swapModel, ILogger log)
        {
            // get the employee object for the user that requested a swap
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapModel.SenderId).ConfigureAwait(false);
            if (employee == null)
            {
                return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
            }

            var essClient = CreateEssPublicClient(employee);

            var request = new ShiftSwapRequest
            {
                SwapperScheduledShiftId = int.Parse(swapModel.SenderShiftId),
                RequestedScheduledShiftIds = new List<int?> { int.Parse(swapModel.RecipientShiftId) }
            };

            try
            {
                var swapResponse = await essClient.CreateShiftSwapRequestAsync(request).ConfigureAwait(false);
                if (swapResponse is ErrorResponse)
                {
                    var errorResponse = (ErrorResponse)swapResponse;
                    var errorCode = errorResponse.Errors[0].ErrorCode;
                    return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
                }

                // BY supports requesting multiple shifts in a single swap request, teams does not
                // therefore we can always take the first shift from the collection
                var response = (ShiftSwapResponse)swapResponse;
                swapModel.SwapRequestId = response.Entities[0].SwapRequestId;
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, swapModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }

            return WfmResponse.SuccessResponse();
        }

        public async Task<WfmResponse> RecipientApproveShiftSwapRequestAsync(WfmShiftSwapModel swapModel, bool approve, ILogger log)
        {
            // get the employee object for the user that recieved the swap request
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapModel.RecipientId).ConfigureAwait(false);
            if (employee == null)
            {
                return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
            }

            var essClient = CreateEssPublicClient(employee);

            var request = new SwapShiftRequestResource
            {
                SwapRequestId = swapModel.SwapRequestId,
                SwapperScheduledShiftId = swapModel.SenderShiftId,
                SwappeeScheduledShiftId = swapModel.RecipientShiftId,
                RequestStatus = approve
                    ? "AwaitingManager"
                    : "RecipientDenied"
            };

            try
            {
                var swapResponse = await essClient.RecipientApproveShiftSwapAsync(request, swapModel.SwapRequestId).ConfigureAwait(false);
                if (swapResponse is ErrorResponse)
                {
                    var errorResponse = (ErrorResponse)swapResponse;
                    var errorCode = errorResponse.Errors[0].ErrorCode;
                    return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
                }
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, swapModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }

            return WfmResponse.SuccessResponse();
        }

        public async Task<WfmResponse> ManagerApproveShiftSwapRequestAsync(WfmShiftSwapModel swapModel, bool approve, ILogger log)
        {
            // get the manager object
            var manager = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapModel.ManagerId).ConfigureAwait(false);
            if (manager == null)
            {
                return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
            }

            var client = CreateSiteManagerPublicClient(manager);

            var siteId = swapModel.BuId;
            var recipientShiftId = int.Parse(swapModel.RecipientShiftId);

            var managerApprovals = await client.GetSiteApprovalsAsync(siteId).ConfigureAwait(false);
            var approval = managerApprovals.SwapShiftApprovals.Entities
                .SingleOrDefault(s => s.SwappeeScheduledShiftId == recipientShiftId);

            if (approval == null)
            {
                // there is no manager approval pending for this swap request so it may have already
                // been approved or cancelled within BY
                return WfmResponse.ErrorResponse(BYErrorCodes.NoManagerApprovalPending, _stringLocalizer[BYErrorCodes.NoManagerApprovalPending]);
            }

            try
            {
                var response = await CallManagerShiftSwapApprovalApi(client, approve, siteId, swapModel.SwapRequestId, recipientShiftId.ToString()).ConfigureAwait(false);

                if (response is ErrorResponse)
                {
                    var errorResponse = (ErrorResponse)response;
                    var errorCode = errorResponse.Errors[0].ErrorCode;
                    return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
                }

                return WfmResponse.SuccessResponse();
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, swapModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }
        }

        public async Task<WfmResponse> CancelShiftSwapRequestAsync(WfmShiftSwapModel swapModel, ILogger log)
        {
            // get the employee object for the user that wants to cancel shift swap.
            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, swapModel.SenderId).ConfigureAwait(false);
            if (employee == null)
            {
                return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
            }

            var essClient = CreateEssPublicClient(employee);

            try
            {
                var swapResponse = await essClient.CancelShiftSwapAsync(swapModel.SwapRequestId).ConfigureAwait(false);

                if (swapResponse is ErrorResponse)
                {
                    var errorResponse = (ErrorResponse)swapResponse;
                    var errorCode = errorResponse.Errors[0].ErrorCode;
                    return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
                }
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, swapModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }

            return WfmResponse.SuccessResponse();
        }

        #endregion Shift Swap Requests

        #region Open Shift Requests

        public async Task<WfmResponse> CreateOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftModel, ILogger log)
        {
            try
            {
                // get the employee object for the user who wants to claim the open shift.
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, openShiftModel.SenderId).ConfigureAwait(false);
                if (employee == null)
                {
                    return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
                }

                var client = CreateEssPublicClient(employee);

                var selectedWeekDay = openShiftModel.WfmOpenShift.StartDate.ApplyTimeZoneOffset(openShiftModel.TimeZoneInfoId).AsDateString();

                var openShiftsResponse = await client.GetMyAvailableShiftsForWeekAsync(selectedWeekDay).ConfigureAwait(false);

                var availableOpenShift = openShiftsResponse.Entities.FirstOrDefault(s => s.ShiftId == openShiftModel.WfmOpenShift.WfmShiftId);
                if (availableOpenShift != null)
                {
                    // Open shift request will not appear in BY until the manager approval process
                    // has complete
                    return WfmResponse.SuccessResponse();
                }
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, openShiftModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }
            catch (Exception ex)
            {
                log.LogBlueYonderGeneralException(ex, openShiftModel.BuId, openShiftModel.WfmOpenShift.WfmShiftId, nameof(CreateOpenShiftRequestAsync));
                return WfmResponse.ErrorResponse(BYErrorCodes.InternalError, _stringLocalizer[BYErrorCodes.InternalError]);
            }

            return WfmResponse.ErrorResponse(BYErrorCodes.ShiftNotAvailableToUser, _stringLocalizer[BYErrorCodes.ShiftNotAvailableToUser]);
        }

        public Task<WfmResponse> CancelOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftModel, ILogger log)
        {
            // Blue Yonder do not require Open Shift Requests to be manager approved and therefore
            // there is no 'pending' state awaiting manager approval that can be cancelled.
            return Task.FromResult(WfmResponse.SuccessResponse());
        }

        public async Task<WfmResponse> ManagerApproveOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftRequestModel, bool approve, ILogger log)
        {
            if (!approve)
            {
                // we don't want to approve so as there is nothing to do just return success
                return WfmResponse.SuccessResponse();
            }

            try
            {
                // get the employee object for the user who wants to claim the open shift.
                var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, openShiftRequestModel.SenderId).ConfigureAwait(false);
                if (employee == null)
                {
                    return WfmResponse.ErrorResponse(BYErrorCodes.UserCredentialsNotFound, _stringLocalizer[BYErrorCodes.UserCredentialsNotFound]);
                }

                // BY doesnt have a manager approve stage so we just use this method to request the
                // shift as an employee
                var client = CreateEssPublicClient(employee);
                var retailWebClient = await CreatePublicClientAsync();

                // attempt to claim it
                var claimOpenShiftResponse = await client.RequestOpenShiftAsync(openShiftRequestModel.WfmOpenShift.WfmShiftId).ConfigureAwait(false);
                if (claimOpenShiftResponse is ErrorResponse errorResponse)
                {
                    var errorCode = errorResponse.Errors[0].ErrorCode;
                    return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
                }

                var updatedOpenShift = claimOpenShiftResponse as RequestOpenShiftResponse;

                // get the id of the new shift that has been created from the open shift assignment
                var newShiftId = await GetEmployeeNewShiftId(employee, openShiftRequestModel.WfmOpenShift, openShiftRequestModel.TimeZoneInfoId, retailWebClient);

                return WfmResponse.SuccessResponse(newShiftId);
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, openShiftRequestModel.BuId);
                return WfmResponse.ErrorResponse(BYErrorCodes.UserUnauthorized, _stringLocalizer[BYErrorCodes.UserUnauthorized]);
            }
            catch (Exception ex)
            {
                log.LogBlueYonderGeneralException(ex, openShiftRequestModel.BuId, openShiftRequestModel.WfmOpenShift.WfmShiftId, nameof(ManagerApproveOpenShiftRequestAsync));
                return WfmResponse.ErrorResponse(BYErrorCodes.InternalError, _stringLocalizer[BYErrorCodes.InternalError]);
            }
        }

        private async Task<string> GetEmployeeNewShiftId(EmployeeModel employee, ShiftModel openShift, string timeZoneInfoId, IBlueYonderClient retailWebClient)
        {
            // now get the user's shifts for the given business date after the assignment is
            // successful to find the newly created shift by start time
            var openShiftStartTime = openShift.StartDate.ApplyTimeZoneOffset(timeZoneInfoId);
            var employeeShifts = await retailWebClient.GetEmployeeShiftsForBusinessDateAsync(int.Parse(employee.WfmEmployeeId), openShiftStartTime.AsDateString()).ConfigureAwait(false);

            // BY does not allow overlapping shifts and therefore we expect only 1 shift with
            // matching start time
            var newShift = employeeShifts.Entities.Single(e => e.StartTime == openShiftStartTime);

            return newShift.ScheduledShiftId.ToString();
        }

        public async Task<WfmResponse> ManagerAssignOpenShiftAsync(ShiftModel assignedOpenShift, EmployeeModel manager, EmployeeModel employee, string storeId, string timeZoneInfoId, ILogger log)
        {
            var client = await CreatePublicClientAsync().ConfigureAwait(false);

            // Create the request body for assigning the open shift
            var openShiftId = int.Parse(assignedOpenShift.WfmShiftId);
            var request = new AssignOpenShiftRequest
            {
                ScheduledShiftId = openShiftId,
                EmployeeId = int.Parse(employee.WfmEmployeeId)
            };

            var response = await client.AssignOpenShiftAsync(request, openShiftId).ConfigureAwait(false);
            if (response is AlternativeErrorResponse)
            {
                var errorResponse = (AlternativeErrorResponse)response;
                var errorCode = errorResponse.ErrorCode;
                return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
            }

            // get the id of the new shift that has been created from the open shift assignment
            var newShiftId = await GetEmployeeNewShiftId(employee, assignedOpenShift, timeZoneInfoId, client);

            return WfmResponse.SuccessResponse(newShiftId);
        }

        #endregion Open Shift Requests

        #region Shift Preference Requests

        public async Task<WfmResponse> UpdateEmployeeAvailabilityAsync(EmployeeAvailabilityModel availabilityModel, ILogger log)
        {
            var client = await CreatePublicClientAsync().ConfigureAwait(false);
            var employeeId = int.Parse(availabilityModel.WfmEmployeeId);

            // get the current availability from Blue Yonder for this user
            var availability = await client.GetEmployeeAvailabilityAsync(employeeId).ConfigureAwait(false);
            // map the availability from Teams into it
            var currentAvailability = _availabilityMap.MapAvailability(availability, availabilityModel);

            try
            {
                await client.CreateEmployeeAvailabilityAsync(currentAvailability, employeeId).ConfigureAwait(false);
            }
            catch (HttpOperationException hex) when (hex.Response?.StatusCode == HttpStatusCode.Conflict)
            {
                var errorResponse = JsonConvert.DeserializeObject<CommonErrorResponse>(hex.Response.Content);
                log.LogShiftPreferenceChangeError(hex, availabilityModel, errorResponse.ErrorCode);
                string errorCode = MapAvailabilityErrorCode(errorResponse.ErrorCode);
                return WfmResponse.ErrorResponse(errorCode, _stringLocalizer[errorCode]);
            }
            catch (Exception ex)
            {
                log.LogShiftPreferenceChangeError(ex, availabilityModel);
                return WfmResponse.ErrorResponse(BYErrorCodes.InternalError, _stringLocalizer[BYErrorCodes.InternalError]);
            }

            return WfmResponse.SuccessResponse();
        }

        #endregion Shift Preference Requests

        private string MapAvailabilityErrorCode(string errorCode)
        {
            return errorCode switch
            {
                "EmployeeAvailabilityErrorCodes.EffEndMustBeAfterEffStart" => BYErrorCodes.AvailabilityEndMustBeAfterStart,
                "EmployeeAvailabilityErrorCodes.NumberOfWeeksMustBe1OrGreater" => BYErrorCodes.AvailabilityNumWeeksMustBeGreaterThanZero,
                "EmployeeAvailabilityErrorCodes.AvailabilityWeekNumberExceedsCycleLength" => BYErrorCodes.AvailabilityWeekNumberGreaterThanCycleLength,
                "EmployeeAvailabilityErrorCodes.AvailabilityWeekNumberLessThan1" => BYErrorCodes.AvailabilityWeekNumberLessThanOne,
                "EmployeeAvailabilityErrorCodes.AvailabilityPreferredAvailabilityMustBeContainedInGeneral" => BYErrorCodes.AvailabilityPreferenceMustBeInGeneral,
                "EmployeeAvailabilityErrorCodes.AvailabilityCannotHaveSameKey" => BYErrorCodes.AvailabilityNoChange,
                "EmployeeAvailabilityErrorCodes.ScheduleRequirementCycleBaseDateMustNotBeLaterThanStartDateRule" => BYErrorCodes.AvailabilityCycleBaseDateMustBeLessOrEqualStartDate,
                "EmployeeAvailabilityErrorCodes.AvailabilityTimeRangesShouldBeIn15MinutesIncrement" => BYErrorCodes.AvailabilityTimeRangesMustBeInFifteenMinIncrements,
                _ => BYErrorCodes.UnknownErrorCode,
            };
        }

        private Task<object> CallManagerShiftSwapApprovalApi(IBlueYonderClient client, bool approve, string siteId, string swapRequestId, string recipientShiftId)
        {
            if (approve)
            {
                // We want to attempt to approve the shift swap request
                return client.ApproveShiftSwapAsync(siteId, swapRequestId, recipientShiftId);
            }

            // We want to attempt to deny the shift swap request
            return client.DenyShiftSwapAsync(siteId, swapRequestId, recipientShiftId);
        }
    }
}
