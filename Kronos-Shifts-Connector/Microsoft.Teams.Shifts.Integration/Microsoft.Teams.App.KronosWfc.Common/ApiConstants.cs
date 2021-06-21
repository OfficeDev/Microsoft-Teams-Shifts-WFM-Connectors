// <copyright file="ApiConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Common
{
    /// <summary>
    /// API Constants class.
    /// </summary>
    public static class ApiConstants
    {
        /// <summary>
        /// Used as an opening braces to send post soap requests.
        /// </summary>
        public const string SoapEnvOpen = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:hs=""http://localhost/wfc/XMLAPISchema"" ><soapenv:Body><hs:KronosWFC><Kronos_WFC version = ""1.0"">";

        /// <summary>
        /// Used as an closing braces to send post soap requests.
        /// </summary>
        public const string SoapEnvClose = @"</Kronos_WFC></hs:KronosWFC></soapenv:Body></soapenv:Envelope>";

        /// <summary>
        /// Used as headers to perform post xml requests.
        /// </summary>
        public const string SoapAction = "http://localhost/wfc/XMLAPISchema";

        /// <summary>
        /// Defines the object for creating Log on requests.
        /// </summary>
        public const string System = "System";

        /// <summary>
        /// Defines the action for creating Log on requests.
        /// </summary>
        public const string LogonAction = "Logon";

        /// <summary>
        /// Defines the local name of the xml response.
        /// </summary>
        public const string Response = "Response";

        /// <summary>
        /// Defines the Action to create Load requests.
        /// </summary>
        public const string LoadAction = "Load";

        /// <summary>
        /// Defines the Action to create RunQuery requests.
        /// </summary>
        public const string RunQueryAction = "RunQuery";

        /// <summary>
        /// Defines the Action to create LoadAllQueries requests.
        /// </summary>
        public const string LoadAllQueries = "LoadAllQueries";

        /// <summary>
        /// Defines the visibility status for Kronos side.
        /// </summary>
        public const string PublicVisibilityCode = "Public";

        /// <summary>
        /// Defines the success status from Kronos side.
        /// </summary>
        public const string Success = "Success";

        /// <summary>
        /// Defines the failure status from Kronos side.
        /// </summary>
        public const string Failure = "Failure";

        /// <summary>
        /// Defines UserNotLoggedInError Code.
        /// </summary>
        public const string UserNotLoggedInError = "1307";

        /// <summary>
        /// Defines the Action to create requests with details.
        /// </summary>
        public const string RetrieveWithDetails = "RetrieveWithDetails";

        /// <summary>
        /// Defines the Action to create open shift requests.
        /// </summary>
        public const string LoadOpenShifts = "LoadOpenShifts";

        /// <summary>
        /// Defines the request status for time off.
        /// </summary>
        public const string SubmitRequests = "SubmitRequests";

        /// <summary>
        /// Defines the Action to create submit requests.
        /// </summary>
        public const string AddRequests = "AddRequests";

        /// <summary>
        /// Defines the Action to approve requests.
        /// </summary>
        public const string ApproveRequests = "ApproveRequests";

        /// <summary>
        /// Defines the Action to deny requests.
        /// </summary>
        public const string RefuseRequests = "RefuseRequests";

        /// <summary>
        /// Defines the pending status when migrating the data from Kronos to Shifts.
        /// </summary>
        public const string Pending = "pending";

        /// <summary>
        /// Defines the pending status when migrating the data from Shifts to Kronos.
        /// </summary>
        public const string ShiftsPending = "Pending";

        /// <summary>
        /// Defines the approved status when migrating the data from Shifts to Kronos.
        /// </summary>
        public const string ShiftsApproved = "Approved";

        /// <summary>
        /// Defines the declined status when migrating the data from Shifts to Kronos.
        /// </summary>
        public const string ShiftsDeclined = "Declined";

        /// <summary>
        /// Defines string for updating the status.
        /// </summary>
        public const string UpdateStatus = "UpdateStatus";

        /// <summary>
        /// Defines string for the OpenShiftRequest.
        /// </summary>
        public const string OpenShiftRequest = "Open Shift - Request";

        /// <summary>
        /// Defines the action string for an API action.
        /// </summary>
        public const string Retrieve = "Retrieve";

        /// <summary>
        /// Defines the status to be changed from submitted to offered in Kronos side.
        /// </summary>
        public const string Offered = "OFFERED";

        /// <summary>
        /// Defines the submitted status to fetch submitted entries from Kronos.
        /// </summary>
        public const string Submitted = "SUBMITTED";

        /// <summary>
        /// Defines the approved status to fetch approved entries from Kronos.
        /// </summary>
        public const string ApprovedStatus = "APPROVED";

        /// <summary>
        /// Defines the Refused status to fetch declined entries from Kronos.
        /// </summary>
        public const string Refused = "REFUSED";

        /// <summary>
        /// Defines the Retract status to fetch deleted entries from Kronos.
        /// </summary>
        public const string Retract = "RETRACT";

        /// <summary>
        /// Defines the color to render for Open Shifts when migrating the data from Kronos to Shifts.
        /// </summary>
        public const string Declined = "Declined";

        /// <summary>
        /// Defines the recipient assigned for Shifts to Kronos.
        /// </summary>
        public const string ShiftsRecipient = "Recipient";

        /// <summary>
        /// Defines the recipient assigned for Shifts to Kronos.
        /// </summary>
        public const string ShiftsManager = "Manager";

        /// <summary>
        /// Defines the Laod all payloads status.
        /// </summary>
        public const string LoadAllPayCodes = "LoadAllPayCodes";

        /// <summary>
        /// Defines the Retracted status when fetching deleted data from Kronos.
        /// </summary>
        public const string Retracted = "RETRACTED";

        /// <summary>
        /// Defines the Assigned To status when migrating the data from Kronos to Shifts.
        /// </summary>
        public const string Manager = "manager";

        /// <summary>
        /// Defines the Request for property for global time off Item.
        /// </summary>
        public const string TOR = "TOR";

        /// <summary>
        /// Defines the full day duration of time off.
        /// </summary>
        public const string FullDayDuration = "FULL_DAY";

        /// <summary>
        /// Defines the hours duration of time off.
        /// </summary>
        public const string HoursDuration = "HOURS";

        /// <summary>
        /// Defines string for the SwapShiftRequest.
        /// </summary>
        public const string SwapShiftRequest = "Shift Swap Request";

        /// <summary>
        /// Defines swap Shift comment if Kronos comment is null.
        /// </summary>
        public const string SwapShiftComment = "Other reason";

        /// <summary>
        /// Defines swap Shift comment if Kronos comment is null.
        /// </summary>
        public const string SwapShiftNoteText = "Add";

        /// <summary>
        /// Defines Kronos acceptable date format.
        /// </summary>
        public const string KronosAcceptableDateFormat = "MM/d/yyyy hh:mmtt";

        /// <summary>
        /// Defines the status when FLW1 cancels the Swap Shift Request.
        /// </summary>
        public const string SwapShiftCancelled = "Cancelled";

        /// <summary>
        /// Error message when Kronos login is failed.
        /// </summary>
        public const string KronosFailedLogin = "Unable to login to Kronos. Please check credentials and try again.";

        /// <summary>
        /// Defines the action for loading eligible employees.
        /// </summary>
        public const string LoadEligibleEmployees = "LoadEligibleEmployees";
    }
}