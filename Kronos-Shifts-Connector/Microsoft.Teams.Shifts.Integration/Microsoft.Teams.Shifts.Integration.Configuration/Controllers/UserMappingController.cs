// <copyright file="UserMappingController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using ClosedXML.Excel;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.JobAssignment;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions.Alerts;
    using Microsoft.Teams.Shifts.Integration.Configuration.Helper;
    using Microsoft.Teams.Shifts.Integration.Configuration.Models;
    using Logon = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon;
    using Models = Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// The User Mapping controller.
    /// </summary>
    [Authorize]
    public class UserMappingController : Controller
    {
        private readonly IHyperFindActivity hyperFindActivity;
        private readonly AppSettings appSettings;
        private readonly IGraphUtility graphUtility;
        private readonly ILogonActivity logonActivity;
        private readonly TelemetryClient telemetryClient;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IJobAssignmentActivity jobAssignmentActivity;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly Utility utility;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMappingController"/> class.
        /// </summary>
        /// <param name="appSettings">Application settings DI.</param>
        /// <param name="graphUtility">Graph utility methods DI.</param>
        /// <param name="logonActivity">Kronos Logon Activity DI.</param>
        /// <param name="hyperFindActivity">Kronos Hyper Find Activity DI.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="userMappingProvider">User Mapping provider DI.</param>
        /// <param name="teamDepartmentMappingProvider">Team Department Mapping provider DI.</param>
        /// <param name="configurationProvider">Configuration provider DI.</param>
        /// <param name="jobAssignmentActivity">Kronos job assignment activity DI.</param>
        /// <param name="environment">Hosting environment DI.</param>
        /// <param name="utility">Common utility class DI.</param>
        public UserMappingController(
            AppSettings appSettings,
            IGraphUtility graphUtility,
            ILogonActivity logonActivity,
            IHyperFindActivity hyperFindActivity,
            TelemetryClient telemetryClient,
            IUserMappingProvider userMappingProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IConfigurationProvider configurationProvider,
            IJobAssignmentActivity jobAssignmentActivity,
            IHostingEnvironment environment,
            Utility utility)
        {
            this.appSettings = appSettings;
            this.graphUtility = graphUtility;
            this.logonActivity = logonActivity;
            this.hyperFindActivity = hyperFindActivity;
            this.telemetryClient = telemetryClient;
            this.userMappingProvider = userMappingProvider;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.configurationProvider = configurationProvider;
            this.jobAssignmentActivity = jobAssignmentActivity;
            this.hostingEnvironment = environment;
            this.utility = utility;
        }

        /// <summary>
        /// The landing page.
        /// </summary>
        /// <returns>The default landing page.</returns>
        [HttpGet]
        public ActionResult Index()
        {
            var model = new UserMappingViewModel();
            if (this.User.Identity.IsAuthenticated)
            {
                this.ViewBag.EmailId = this.User.Identity.Name;
                this.ViewBag.IsUserLoggedIn = true;
            }
            else
            {
                this.ViewBag.IsUserLoggedIn = false;
            }

            return this.View(model);
        }

        /// <summary>
        /// The method will get the necessary configurations that have been saved.
        /// </summary>
        /// <returns>A list of the configurations.</returns>
        [HttpGet]
        public async Task<ActionResult> GetUserMappingAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            var mappedUsersResult = await this.userMappingProvider.GetAllMappedUserDetailsAsync().ConfigureAwait(false);
            var mappedUsersResultViewModel = mappedUsersResult.Select(
                m => new AllUserMappingEntity
                {
                    PartitionKey = Utility.OrgJobPathKronosConversion(m.PartitionKey),
                    RowKey = m.RowKey,
                    KronosUserName = m.KronosUserName,
                    ShiftUserAadObjectId = m.ShiftUserAadObjectId,
                    ShiftUserDisplayName = m.ShiftUserDisplayName,
                    ShiftUserUpn = m.ShiftUserUpn,
                }).ToList();
            return this.PartialView("_DataTable", mappedUsersResultViewModel);
        }

        /// <summary>
        /// Method to go back to the previous page.
        /// </summary>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public IActionResult GoBack()
        {
            return this.RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Action method to Export data in Excel format.
        /// </summary>
        /// <returns>An action result where the data is being exported.</returns>
        public async Task<IActionResult> ExportToExcelAsync()
        {
            // Gets the list of all Shift users.
            var shiftResponse = await this.GetAllShiftUsersAsync().ConfigureAwait(false);

            // Gets the list of all Kronos users.
            var kronosResonse = await this.GetAllKronosUsersByQueryAsync().ConfigureAwait(false);
            if (kronosResonse != null)
            {
                // Converting the users list to DataTable.
                var exportDt = ConvertModelToDataTable(shiftResponse, kronosResonse);

                string fileName = Resources.KronosShiftUserMappingXML;

                // returning the excel file into browser as stream.
                using (var stream = ExportImportHelper.ExportToExcel(exportDt))
                {
                    return this.File(stream.ToArray(), Resources.SpreadsheetContentType, fileName);
                }
            }
            else
            {
                return this.RedirectToAction("Index", "UserMapping").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.WorkforceIntegrationNotRegister);
            }
        }

        /// <summary>
        /// Navigation to the next page.
        /// </summary>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public IActionResult GoToNext()
        {
            return this.RedirectToAction("Index", "TeamDepartmentMapping");
        }

        /// <summary>
        /// Method to Download template for UserToUserMapping.
        /// </summary>
        /// <returns>File to browser's response.</returns>
        public async Task<ActionResult> DownloadTemplateAsync()
        {
            using (var memStream = await AzureStorageHelper.DownloadFileFromBlobAsync(
                    this.appSettings.StorageConnectionString,
                    this.appSettings.TemplatesContainerName,
                    this.appSettings.KronosShiftUserMappingTemplateName,
                    this.telemetryClient).ConfigureAwait(false))
            {
                return this.File(memStream.ToArray(), this.appSettings.ExcelContentType, this.appSettings.KronosShiftUserMappingTemplateName);
            }
        }

        /// <summary>
        /// method to import Kronos and Shift users.
        /// </summary>
        /// <returns> returns true if file import is successfull.</returns>
        [HttpPost]
        public async Task<ActionResult> ImportMappingAsync()
        {
            // getting the posted file.
            var file = this.HttpContext.Request.Form.Files[0];

            bool isValidFile = true;
            int noOfColumns = 0;

            if (file != null)
            {
                using (XLWorkbook workbook = new XLWorkbook(file.OpenReadStream()))
                {
                    var worksheet = workbook.Worksheet(1);

                    // validation to check if row other than column exists
                    if (worksheet.RowsUsed().Count() == 1)
                    {
                        isValidFile = false;
                        return this.Json(new { response = isValidFile });
                    }

                    // getting count of total used cells
                    var usedCellsCount = worksheet.RowsUsed().CellsUsed().Count();

                    foreach (IXLRow row in worksheet.RowsUsed())
                    {
                        if (row.RangeAddress.FirstAddress.RowNumber == 1)
                        {
                            // getting count of total coumns available in the template
                            noOfColumns = row.CellsUsed().Count();
                            continue;
                        }

                        // validation to check if any cell has empty value
                        if ((usedCellsCount % noOfColumns) != 0 || noOfColumns != Convert.ToInt16(Resources.NoOfColumnsInExcel, CultureInfo.InvariantCulture))
                        {
                            isValidFile = false;
                            return this.Json(new { response = isValidFile });
                        }

                        AllUserMappingEntity entity = new AllUserMappingEntity()
                        {
                            PartitionKey = Utility.OrgJobPathDBConversion(row.Cell(1).Value.ToString()),
                            RowKey = row.Cell(2).Value.ToString(),
                            KronosUserName = row.Cell(3).Value.ToString(),
                            ShiftUserAadObjectId = row.Cell(4).Value.ToString(),
                            ShiftUserDisplayName = row.Cell(5).Value.ToString(),
                            ShiftUserUpn = row.Cell(6).Value.ToString(),
                        };

                        if (isValidFile)
                        {
                            isValidFile = await this.userMappingProvider.KronosShiftUsersMappingAsync(entity).ConfigureAwait(false);
                        }
                    }
                }
            }

            return this.Json(new { response = isValidFile });
        }

        /// <summary>
        /// Method to delete mapping record from User Mapping table.
        /// </summary>
        /// <param name="partitionKey">Partition Key i.e. OrgJobPath of Kronos.</param>
        /// <param name="rowKey">Kronos Person number.</param>
        /// <returns>Json result indicating success or failure conditions.</returns>
        [HttpPost]
        public async Task<JsonResult> DeleteUserMappingAsync(string partitionKey, string rowKey)
        {
            bool isDeleted = false;
            if (partitionKey != null && rowKey != null)
            {
                isDeleted = await this.userMappingProvider.DeleteMappedUserDetailsAsync(Utility.OrgJobPathDBConversion(partitionKey), rowKey).ConfigureAwait(false);
            }

            if (isDeleted)
            {
                return this.Json(new { response = isDeleted });
            }
            else
            {
                return this.Json(new { response = isDeleted });
            }
        }

        /// <summary>
        /// Method to convert the model to DataTable.
        /// </summary>
        /// <param name="shiftList">List of Shifts users.</param>
        /// <param name="kronosList">List of Kronos users.</param>
        /// <returns>File to browser's response.</returns>
        private static List<DataTable> ConvertModelToDataTable(List<ShiftUser> shiftList, List<KronosUserModel> kronosList)
        {
            DataTable shiftDt = new DataTable();
            DataTable kronosDt = new DataTable();

            try
            {
                // Setting table names.
                string kronosDtName = "Kronos Users";
                string shiftDtName = "Shift Users";

                shiftDt = ExportImportHelper.ToDataTable(shiftList, shiftDtName);
                kronosDt = ExportImportHelper.ToDataTable(kronosList, kronosDtName);

                List<DataTable> listDt = new List<DataTable>()
                {
                    shiftDt,
                    kronosDt,
                };

                return listDt;
            }
            finally
            {
                if (shiftDt != null)
                {
                    ((IDisposable)shiftDt).Dispose();
                }

                if (kronosDt != null)
                {
                    ((IDisposable)kronosDt).Dispose();
                }
            }
        }

        /// <summary>
        /// Gets Users from Shift using Admin authorization.
        /// </summary>
        /// <returns>A task.</returns>
        private async Task<List<ShiftUser>> GetAllShiftUsersAsync()
        {
            var clientId = this.appSettings.ClientId;
            var instance = this.appSettings.Instance;
            var clientSecret = this.appSettings.ClientSecret;

            var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var tenantId = this.User.GetTenantId();

            var graphAccessToken = await this.graphUtility.GetAccessTokenAsync(
                tenantId,
                instance,
                clientId,
                clientSecret,
                userId).ConfigureAwait(false);

            List<ShiftUser> teamsUserModels = new List<ShiftUser>();

            teamsUserModels = await this.graphUtility.FetchShiftUserDetailsAsync(
                graphAccessToken).ConfigureAwait(false);

            return teamsUserModels;
        }

        /// <summary>
        /// Gets Users from Kronos using Hyperfind Query.
        /// </summary>
        /// <returns>A task.</returns>
        private async Task<List<KronosUserModel>> GetAllKronosUsersByQueryAsync()
        {
            // Get configuration info from table.
            var configurationEntity = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false)).FirstOrDefault();
            if (configurationEntity != null && !string.IsNullOrEmpty(configurationEntity.WorkforceIntegrationId))
            {
                List<KronosUserModel> kronosUserModels = new List<KronosUserModel>();

                var kronosUserDetailsQuery = this.appSettings.KronosUserDetailsQuery;

                // TODO: Save the token into cache and then get it from cache until expiry, to avoid network calls.
                var loginKronos = this.logonActivity.LogonAsync(
                    this.appSettings.WfmSuperUsername,
                    this.appSettings.WfmSuperUserPassword,
                    new Uri(configurationEntity?.WfmApiEndpoint));

                var loginKronosResult = await loginKronos.ConfigureAwait(false);

                var tenantId = this.User.GetTenantId();

                var hyperFindResponse = await this.GetKronosUsersAsync(
                   loginKronosResult,
                   configurationEntity,
                   kronosUserDetailsQuery).ConfigureAwait(false);

                foreach (var element in hyperFindResponse.HyperFindResult)
                {
                    var jobAssigmentResponse = await this.jobAssignmentActivity.GetJobAssignmentAsync(
                        new Uri(configurationEntity.WfmApiEndpoint),
                        element.PersonNumber,
                        tenantId,
                        loginKronosResult.Jsession).ConfigureAwait(false);
                    if (jobAssigmentResponse != null)
                    {
                        var jobDetails = jobAssigmentResponse.JobAssign.PrimaryLaborAccList.PrimaryLaborAcc.OrganizationPath;

                        kronosUserModels.Add(
                            new KronosUserModel()
                            {
                                KronosOrgJobPath = jobDetails,
                                KronosPersonNumber = element.PersonNumber,
                                KronosUserName = element.FullName,
                            });
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"Unable to fetch job assignment for {element.PersonNumber} ");
                    }
                }

                return kronosUserModels;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets Users from Kronos.
        /// </summary>
        /// <param name="result">Logon response.</param>
        /// <param name="configurationEntity">Configuration entity.</param>
        /// <param name="kronosDept">Kronos Department.</param>
        /// <returns>A task.</returns>
        private async Task<Response> GetKronosUsersAsync(
            Logon.Response result,
            Models.ConfigurationEntity configurationEntity,
            string kronosDept)
        {
            var hyperFindResponse = await this.hyperFindActivity.GetHyperFindQueryValuesAsync(
                new Uri(configurationEntity.WfmApiEndpoint),
                configurationEntity.TenantId,
                result.Jsession,
                DateTime.Now.ToString("M/dd/yyyy", CultureInfo.InvariantCulture),
                DateTime.Now.ToString("M/dd/yyyy", CultureInfo.InvariantCulture),
                kronosDept,
                ApiConstants.PublicVisibilityCode).ConfigureAwait(false);

            if (hyperFindResponse?.Status == ApiConstants.Failure)
            {
                // User is not logged in
                if (hyperFindResponse.Error?.ErrorCode == ApiConstants.UserNotLoggedInError)
                {
                    // Get the token and cache it
                    await this.logonActivity.LogonAsync(
                        this.appSettings.WfmSuperUsername,
                        this.appSettings.WfmSuperUserPassword,
                        new Uri(configurationEntity?.WfmApiEndpoint)).ConfigureAwait(false);
                }
                else
                {
                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}, Error from Kronos is: {hyperFindResponse.Error?.Message}");
                }
            }

            return hyperFindResponse;
        }
    }
}