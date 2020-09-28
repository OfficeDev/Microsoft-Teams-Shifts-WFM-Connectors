// <copyright file="TeamDepartmentMappingController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Threading.Tasks;
    using ClosedXML.Excel;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Graph;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFindLoadAll;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions.Alerts;
    using Microsoft.Teams.Shifts.Integration.Configuration.Helper;
    using Microsoft.Teams.Shifts.Integration.Configuration.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// The Shifts team to Kronos department mapping controller.
    /// </summary>
    [Authorize]
    public class TeamDepartmentMappingController : Controller
    {
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly TelemetryClient telemetryClient;
        private readonly ILogonActivity logonActivity;
        private readonly IHyperFindLoadAllActivity hyperFindLoadAllActivity;
        private readonly ShiftsTeamKronosDepartmentViewModel shiftsTeamKronosDepartmentViewModel;
        private readonly IGraphUtility graphUtility;
        private readonly AppSettings appSettings;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IDistributedCache cache;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamDepartmentMappingController"/> class.
        /// </summary>
        /// <param name="teamDepartmentMappingProvider">configurationProvider DI.</param>
        /// <param name="telemetryClient">The logging mechanism through Application Insights.</param>
        /// <param name="logonActivity">Logon activity.</param>
        /// <param name="hyperFindLoadAllActivity">IHyperFindLoadAllActivity object.</param>
        /// <param name="shiftsTeamKronosDepartmentViewModel">ShiftsTeamKronosDepartmentViewModel object.</param>
        /// <param name="graphUtility">Having the ability to DI the mechanism to get the token from MS Graph.</param>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="configurationProvider">ConfigurationProvider DI.</param>
        /// <param name="cache">Distributed cache.</param>
        /// <param name="userMappingProvider">User To User Mapping Provider.</param>
        /// <param name="httpClientFactory">httpClientFactory.</param>
        public TeamDepartmentMappingController(
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            TelemetryClient telemetryClient,
            ILogonActivity logonActivity,
            IHyperFindLoadAllActivity hyperFindLoadAllActivity,
            ShiftsTeamKronosDepartmentViewModel shiftsTeamKronosDepartmentViewModel,
            IGraphUtility graphUtility,
            AppSettings appSettings,
            IConfigurationProvider configurationProvider,
            IDistributedCache cache,
            IUserMappingProvider userMappingProvider,
            IHttpClientFactory httpClientFactory)
        {
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.telemetryClient = telemetryClient;
            this.logonActivity = logonActivity;
            this.hyperFindLoadAllActivity = hyperFindLoadAllActivity;
            this.shiftsTeamKronosDepartmentViewModel = shiftsTeamKronosDepartmentViewModel;
            this.graphUtility = graphUtility;
            this.appSettings = appSettings;
            this.configurationProvider = configurationProvider;
            this.cache = cache;
            this.userMappingProvider = userMappingProvider;
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Default action.
        /// </summary>
        /// <returns>Shifts team Kronos department mapping page.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            this.ViewBag.IsUserLoggedIn = this.User.Identity.IsAuthenticated;
            this.ViewBag.EmailId = this.User.Identity.Name ?? string.Empty;

            return this.View();
        }

        /// <summary>
        /// Method to sync data for the first time.
        /// </summary>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> SyncDataFirstTimeAsync()
        {
            HttpRequestMessage request;

            Task<HttpResponseMessage> syncKronosToShiftsResponse;

            HttpResponseMessage syncKronosToShiftsResponseTask;

            var client = this.httpClientFactory.CreateClient("ShiftsKronosIntegrationAPI");

            string errorMsgs = string.Empty;
            if (client.BaseAddress == null)
            {
                client.BaseAddress = new Uri(this.appSettings.BaseAddressFirstTimeSync);
            }

            client.Timeout = TimeSpan.FromMinutes(30);
            var checkSetUp = await client.GetAsync(new Uri(client.BaseAddress + "api/teams/CheckSetup")).ConfigureAwait(false);

            if (checkSetUp.IsSuccessStatusCode)
            {
                var clientId = this.appSettings.ClientId;
                var instance = this.appSettings.Instance;
                var clientSecret = this.appSettings.ClientSecret;

                var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var tenantId = this.User.GetTenantId();

                var graphAccessToken = await this.graphUtility.GetAccessTokenAsync(tenantId, instance, clientId, clientSecret, userId).ConfigureAwait(false);

                // Get JWT Token for API request
                TokenHelper tokenHelper = new TokenHelper(this.appSettings);
                string apiAccessToken = tokenHelper.GenerateToken();

                // Run sync Kronos to Shifts.
                using (request = this.PrepareHttpRequest("api/SyncKronosToShifts/StartSync/false", graphAccessToken, apiAccessToken))
                {
                    syncKronosToShiftsResponse = client.SendAsync(request);
                    syncKronosToShiftsResponseTask = await syncKronosToShiftsResponse.ConfigureAwait(false);
                    var syncKronosToShiftsStatus = syncKronosToShiftsResponseTask.StatusCode;
                }

                if (!syncKronosToShiftsResponseTask.IsSuccessStatusCode)
                {
                    var syncKronosToShiftsError = await syncKronosToShiftsResponseTask.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name} error: " + syncKronosToShiftsError);
                    errorMsgs += Resources.SyncKronosToShiftsError;
                }
            }
            else
            {
                errorMsgs += Resources.SetUpNotDoneMessage;
            }

            if (string.IsNullOrEmpty(errorMsgs))
            {
                return this.RedirectToAction("Index").WithSuccess(Resources.SuccessNotificationHeaderText, Resources.SetUpSuccessfulMessage);
            }
            else
            {
                return this.RedirectToAction("Index").WithErrorMessage(Resources.ErrorNotificationHeaderText, errorMsgs);
            }
        }

        /// <summary>
        /// The method will get the necessary configurations that have been saved.
        /// </summary>
        /// <returns>A list of the configurations.</returns>
        [HttpGet]
        public async Task<ActionResult> GetTeamDepartmentMappingAsync()
        {
            var mappedTeamsResult = await this.teamDepartmentMappingProvider.GetMappedTeamToDeptsWithJobPathsAsync().ConfigureAwait(false);
            var mappedTeamsResultViewModel = mappedTeamsResult.Select(
                m => new TeamsDepartmentMappingViewModel
                {
                    WorkforceIntegrationId = m.PartitionKey,
                    KronosOrgJobPath = Utility.OrgJobPathKronosConversion(m.RowKey),
                    KronosTimeZone = m.KronosTimeZone,
                    ShiftsTeamName = m.ShiftsTeamName,
                    TeamId = m.TeamId,
                    TeamsScheduleGroupId = m.TeamsScheduleGroupId,
                    TeamsScheduleGroupName = m.TeamsScheduleGroupName,
                }).ToList();
            return this.PartialView("_DataTable", mappedTeamsResultViewModel);
        }

        /// <summary>
        /// Navigation to the home page.
        /// </summary>
        /// <returns>A type of <see cref="IActionResult"/>.</returns>
        public IActionResult GoBack()
        {
            return this.RedirectToAction("Index", "UserMapping");
        }

        /// <summary>
        /// Method to Download template for UserToUserMapping.
        /// </summary>
        /// <returns>File to browser's response.</returns>
        public async Task<ActionResult> DownloadTemplateAsync()
        {
            using (MemoryStream memStream = await Helper.AzureStorageHelper.DownloadFileFromBlobAsync(
                     this.appSettings.StorageConnectionString,
                     this.appSettings.TemplatesContainerName,
                     this.appSettings.KronosShiftTeamDeptMappingTemplateName,
                     this.telemetryClient).ConfigureAwait(false))
            {
                return this.File(memStream.ToArray(), this.appSettings.ExcelContentType, this.appSettings.KronosShiftTeamDeptMappingTemplateName);
            }
        }

        /// <summary>
        /// Action method to Export data in Excel format.
        /// </summary>
        /// <returns>An action result with the exported.</returns>
        public async Task<IActionResult> ExportToExcelAsync()
        {
            // Fetching the workforce integration id
            var wfi = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false)).FirstOrDefault();

            if (wfi != null && !string.IsNullOrEmpty(wfi.WorkforceIntegrationId))
            {
                var clientId = this.appSettings.ClientId;
                var instance = this.appSettings.Instance;
                var clientSecret = this.appSettings.ClientSecret;

                var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var tenantId = this.User.GetTenantId();

                var graphAccessToken = await this.graphUtility.GetAccessTokenAsync(tenantId, instance, clientId, clientSecret, userId).ConfigureAwait(false);

                // Fetching the list of all distinct KronosOrgJobPaths
                var kronosOrgJobPathList = await this.userMappingProvider.GetDistinctOrgJobPatAsync().ConfigureAwait(false);

                List<KronosDetails> kronosDetailsList = new List<KronosDetails>();

                foreach (var orgJobPath in kronosOrgJobPathList)
                {
                    kronosDetailsList.Add(new KronosDetails { WorkforceIntegrationId = wfi.WorkforceIntegrationId, KronosOrgJobPath = Utility.OrgJobPathKronosConversion(orgJobPath) });
                }

                // Fetching the shift team details
                var shiftTeamsList = await this.graphUtility.FetchShiftTeamDetailsAsync(graphAccessToken).ConfigureAwait(false);

                // Iterating the shift teams list to fetch the scheduling groups corrsponding to the shift teams id
                foreach (var shiftTeam in shiftTeamsList)
                {
                    // Fetching the scheduling group details
                    var shiftSchedulingGroupDetailsResponse = await this.graphUtility.FetchSchedulingGroupDetailsAsync(graphAccessToken, shiftTeam.ShiftTeamId).ConfigureAwait(false);

                    var groupResponse = JsonConvert.DeserializeObject<SchedulingGroupDetails>(shiftSchedulingGroupDetailsResponse);

                    shiftTeam.SchedulingGroups = new List<ShiftSchedulingGroups>();
                    if (groupResponse != null && groupResponse.Value != null)
                    {
                        shiftTeam.SchedulingGroups = groupResponse.Value.Select(element =>
                              new ShiftSchedulingGroups
                              {
                                  ShiftSchedulingGroupId = element.ShiftSchedulingGroupId,
                                  ShiftSchedulingGroupName = element.ShiftSchedulingGroupName,
                              }).ToList();
                    }
                }

                List<DataTable> exportDt = ConvertModelToDataTable(shiftTeamsList, kronosDetailsList);

                string fileName = Resources.TeamsDepartmentMappingXML;

                // Returning the excel file into browser as stream
                using (MemoryStream stream = Helper.ExportImportHelper.ExportToExcel(exportDt))
                {
                    return this.File(stream.ToArray(), Resources.SpreadsheetContentType, fileName);
                }
            }
            else
            {
                return this.RedirectToAction("Index", "TeamDepartmentMapping").WithErrorMessage(Resources.ErrorNotificationHeaderText, Resources.WorkforceIntegrationNotRegister);
            }
        }

        /// <summary>
        /// method to import Teams and Departments details.
        /// </summary>
        /// <returns>Json result indicating success or failure conditions.</returns>
        [HttpPost]
        public async Task<IActionResult> ImportMappingAsync()
        {
            var configurationEntities = await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false);
            var configEntity = configurationEntities?.FirstOrDefault();

            if (configEntity != null && !string.IsNullOrEmpty(configEntity.WorkforceIntegrationId))
            {
                // Getting the posted file.
                var file = this.HttpContext.Request.Form.Files[0];

                bool isValidFile = true;
                int noOfColumns = 0;

                if (file != null)
                {
                    using (XLWorkbook workbook = new XLWorkbook(file.OpenReadStream()))
                    {
                        IXLWorksheet worksheet = workbook.Worksheet(1);

                        // Validation to check if row other than column exists
                        if (worksheet.RowsUsed().Count() == 1)
                        {
                            isValidFile = false;
                            return this.Json(new { isWorkforceIntegrationPresent = true, response = isValidFile });
                        }

                        // Getting count of total used cells
                        var usedCellsCount = worksheet.RowsUsed().CellsUsed().Count();

                        foreach (IXLRow row in worksheet.RowsUsed())
                        {
                            if (row.RangeAddress.FirstAddress.RowNumber == 1)
                            {
                                // Getting count of total coumns available in the template
                                noOfColumns = row.CellsUsed().Count();
                                continue;
                            }

                            // Validation to check if any cell has empty value
                            if ((usedCellsCount % noOfColumns) != 0 || noOfColumns != Convert.ToInt16(Resources.NoOfColumnsInExcel, CultureInfo.InvariantCulture))
                            {
                                isValidFile = false;
                                return this.Json(new { isWorkforceIntegrationPresent = true, response = isValidFile });
                            }

                            TeamToDepartmentJobMappingEntity entity = new TeamToDepartmentJobMappingEntity()
                            {
                                PartitionKey = row.Cell(1).Value.ToString(),
                                RowKey = Utility.OrgJobPathDBConversion(row.Cell(2).Value.ToString()),
                                KronosTimeZone = row.Cell(3).Value.ToString(),
                                TeamId = row.Cell(4).Value.ToString(),
                                ShiftsTeamName = row.Cell(5).Value.ToString(),
                                TeamsScheduleGroupId = row.Cell(6).Value.ToString(),
                                TeamsScheduleGroupName = row.Cell(7).Value.ToString(),
                            };

                            if (isValidFile)
                            {
                                var tenantId = this.appSettings.TenantId;
                                var clientId = this.appSettings.ClientId;
                                var clientSecret = this.appSettings.ClientSecret;
                                var instance = this.appSettings.Instance;

                                var accessToken = await this.graphUtility.GetAccessTokenAsync(tenantId, instance, clientId, clientSecret, configEntity.AdminAadObjectId).ConfigureAwait(false);
                                var graphClient = CreateGraphClientWithDelegatedAccess(accessToken);

                                var isSuccess = await this.graphUtility.AddWFInScheduleAsync(entity.TeamId, graphClient, configEntity.WorkforceIntegrationId, accessToken).ConfigureAwait(false);
                                if (isSuccess)
                                {
                                    await this.teamDepartmentMappingProvider.TeamsToDepartmentMappingAsync(entity).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }

                return this.Json(new { isWorkforceIntegrationPresent = true, response = isValidFile });
            }
            else
            {
                return this.Json(new { isWorkforceIntegrationPresent = false, error = Resources.WorkforceIntegrationNotRegister });
            }
        }

        /// <summary>
        /// Method to delete mapping record from TeamsDept Mapping table.
        /// </summary>
        /// <param name="partitionKey">Partion key i.e. WorkForceIntegration ID.</param>
        /// <param name="rowKey">Row Key i.e. OrgJobPath of Kronos.</param>
        /// <returns>Json result indicating success or failure conditions.</returns>
        [HttpPost]
        public async Task<ActionResult> DeleteTeamsDeptMappingAsync(string partitionKey, string rowKey)
        {
            bool isDeleted = false;
            if (partitionKey != null && rowKey != null)
            {
                isDeleted = await this.teamDepartmentMappingProvider.DeleteMappedTeamDeptDetailsAsync(partitionKey, Utility.OrgJobPathDBConversion(rowKey)).ConfigureAwait(false);
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
        /// <param name="shiftTeamsList">The list of teams in Shifts.</param>
        /// <param name="kronosOrgJobPathList">The Kronos Details list.</param>
        /// <returns>File to browser's response.</returns>
        private static List<DataTable> ConvertModelToDataTable(
            List<ShiftTeams> shiftTeamsList,
            List<KronosDetails> kronosOrgJobPathList)
        {
            DataTable shiftTeamsDt = new DataTable();
            DataTable kronosOrgJobPathDt = new DataTable();

            // Setting Table Names
            string shiftTeamsDtName = "Shift Team Details";
            string kronosOrgJobPathDtName = "Kronos Details";

            // Columns for Shift details
            shiftTeamsDt.Columns.Add("ShiftTeamId", typeof(string));
            shiftTeamsDt.Columns.Add("ShiftTeamName", typeof(string));
            shiftTeamsDt.Columns.Add("ShiftSchedulingGroupId", typeof(string));
            shiftTeamsDt.Columns.Add("ShiftSchedulingGroupName", typeof(string));

            // Columns for Kronos details
            kronosOrgJobPathDt.Columns.Add("WorkforceIntegrationId", typeof(string));
            kronosOrgJobPathDt.Columns.Add("KronosOrgJobPath", typeof(string));

            try
            {
                shiftTeamsDt = Helper.ExportImportHelper.CreateNestedDataTable<ShiftTeams, ShiftSchedulingGroups>(shiftTeamsList, "SchedulingGroups", shiftTeamsDtName);
                kronosOrgJobPathDt = Helper.ExportImportHelper.ToDataTable(kronosOrgJobPathList, kronosOrgJobPathDtName);

                List<DataTable> listDt = new List<DataTable>()
                {
                shiftTeamsDt,
                kronosOrgJobPathDt,
                };

                return listDt;
            }
            finally
            {
                kronosOrgJobPathDt?.Dispose();
                shiftTeamsDt?.Dispose();
            }
        }

        /// <summary>
        /// Method that creates the Microsoft Graph Service client.
        /// </summary>
        /// <param name="token">The Graph Access token.</param>
        /// <returns>A type of <see cref="GraphServiceClient"/> contained in a unit of execution.</returns>
        private static GraphServiceClient CreateGraphClientWithDelegatedAccess(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(token);
            }

            var graphClient = new GraphServiceClient(
            new DelegateAuthenticationProvider(
                (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return Task.FromResult(0);
                }));
            return graphClient;
        }

        private void GetTenantDetails(
            out string tenantId,
            out string clientId,
            out string clientSecret,
            out string instance)
        {
            tenantId = this.appSettings.TenantId;
            clientId = this.appSettings.ClientId;
            clientSecret = this.appSettings.ClientSecret;
            instance = this.appSettings.Instance;
        }

        /// <summary>
        /// Method to generate the Http Request object with required headers.
        /// </summary>
        /// <param name="apiUrl">API Url to call.</param>
        /// <param name="graphAccessToken">The Microsoft Graph access token.</param>
        /// <param name="apiAccessToken">The JWT Token for API calls.</param>
        /// <returns>HttpRequestMessage object.</returns>
        private HttpRequestMessage PrepareHttpRequest(string apiUrl, string graphAccessToken, string apiAccessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(this.appSettings.BaseAddressFirstTimeSync + apiUrl));
            request.Headers.Add("Authorization", "Bearer " + graphAccessToken);
            request.Headers.Add("AccessToken", "Bearer " + apiAccessToken);
            return request;
        }
    }
}