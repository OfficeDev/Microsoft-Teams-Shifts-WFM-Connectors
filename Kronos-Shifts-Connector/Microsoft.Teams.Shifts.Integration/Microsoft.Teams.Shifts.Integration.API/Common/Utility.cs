// <copyright file="Utility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Newtonsoft.Json;
    using Logon = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon;
    using OpenShift = Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift;

    /// <summary>
    /// This is the utility class to perform several common operations such as Unique Id creation, Date Time operations, tokens fetch, session fecth, conditions checks etc.
    /// </summary>
    public class Utility
    {
        private readonly TelemetryClient telemetryClient;
        private readonly AppSettings appSettings;
        private readonly ILogonActivity logonActivity;
        private readonly Integration.BusinessLogic.Providers.IConfigurationProvider configurationProvider;
        private readonly IDistributedCache cache;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly IGraphUtility graphUtility;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utility"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="logonActivity">Kronos Logon activity DI.</param>
        /// <param name="appSettings">Application Settings DI.</param>
        /// <param name="cache">Distributed cache DI.</param>
        /// <param name="configurationProvider">Configuration provider DI.</param>
        /// <param name="azureTableStorageHelper">Azure table storage DI.</param>
        /// <param name="graphUtility">Graph utility methods DI.</param>
        public Utility(
            TelemetryClient telemetryClient,
            ILogonActivity logonActivity,
            AppSettings appSettings,
            IDistributedCache cache,
            IConfigurationProvider configurationProvider,
            IAzureTableStorageHelper azureTableStorageHelper,
            IGraphUtility graphUtility)
        {
            this.telemetryClient = telemetryClient;
            this.appSettings = appSettings;
            this.cache = cache;
            this.logonActivity = logonActivity;
            this.configurationProvider = configurationProvider;
            this.azureTableStorageHelper = azureTableStorageHelper;
            this.graphUtility = graphUtility;
        }

        /// <summary>
        /// Get month partitons for a given start and end date.
        /// </summary>
        /// <param name="startDate">Start Date of the query.</param>
        /// <param name="endDate">End Date of the query.</param>
        /// <returns>Returns list of month partition.</returns>
        public static List<string> GetMonthPartition(string startDate, string endDate)
        {
            List<string> result = new List<string>();
            var startDateDetails = DateTime.ParseExact(startDate, "M/dd/yyyy", CultureInfo.InvariantCulture);
            var endDateDetails = DateTime.ParseExact(endDate, "M/dd/yyyy", CultureInfo.InvariantCulture);

            int startYear = startDateDetails.Year;
            int endYear = endDateDetails.Year;
            int startMonth = startDateDetails.Month;
            int endMonth = endDateDetails.Month;

            if (endYear - startYear == 0)
            {
                for (int j = 0; j <= endMonth - startMonth; j++)
                {
                    result.Add((startMonth + j).ToString(CultureInfo.InvariantCulture) + "_" + startYear.ToString(CultureInfo.InvariantCulture));
                }

                return result;
            }
            else if (endYear - startYear == 1)
            {
                for (int j = 0; j <= 12 - startMonth; j++)
                {
                    result.Add((startMonth + j).ToString(CultureInfo.InvariantCulture) + "_" + startYear.ToString(CultureInfo.InvariantCulture));
                }

                for (int i = 0; i <= endMonth - 1; i++)
                {
                    result.Add((i + 1).ToString(CultureInfo.InvariantCulture) + "_" + endYear.ToString(CultureInfo.InvariantCulture));
                }

                return result;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// This method will be able to get the next date span.
        /// </summary>
        /// <param name="monthPartitionKey">The month partition key.</param>
        /// <param name="firstMonthPartition">The first month partition.</param>
        /// <param name="lastMonthPartition">The last month partition.</param>
        /// <param name="shiftStartDate">The shift start date.</param>
        /// <param name="shiftEndDate">The shift end date.</param>
        /// <param name="queryStartDate">The start date.</param>
        /// <param name="queryEndDate">The end date.</param>
        public static void GetNextDateSpan(
            string monthPartitionKey,
            string firstMonthPartition,
            string lastMonthPartition,
            string shiftStartDate,
            string shiftEndDate,
            out string queryStartDate,
            out string queryEndDate)
        {
            if (monthPartitionKey == firstMonthPartition && monthPartitionKey == lastMonthPartition)
            {
                queryStartDate = shiftStartDate;
                queryEndDate = shiftEndDate;
            }
            else if (monthPartitionKey == firstMonthPartition)
            {
                queryStartDate = shiftStartDate;
                queryEndDate = GetLastDayInMonth(monthPartitionKey);
            }
            else if (monthPartitionKey == lastMonthPartition)
            {
                queryStartDate = GetFirstDayInMonth(monthPartitionKey);
                queryEndDate = shiftEndDate;
            }
            else
            {
                queryStartDate = GetFirstDayInMonth(monthPartitionKey);
                queryEndDate = GetLastDayInMonth(monthPartitionKey);
            }
        }

        /// <summary>
        /// Get last Day of month.
        /// </summary>
        /// <param name="monthPartition">Month Partition of a query.</param>
        /// <returns>Return last day of Month.</returns>
        public static string GetLastDayInMonth(string monthPartition)
        {
            var monthYear = monthPartition?.Split('_');
            int year = Convert.ToInt16(monthYear.LastOrDefault(), CultureInfo.InvariantCulture);
            int month = Convert.ToInt16(monthYear.FirstOrDefault(), CultureInfo.InvariantCulture);

            DateTime aDateTime = new DateTime(year, month, 1);

            // Add 1 month and substract 1 day.
            DateTime retDateTime = aDateTime.AddMonths(1).AddDays(-1);

            return retDateTime.ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get first Day of month.
        /// </summary>
        /// <param name="monthPartition">Month Partition of a query.</param>
        /// <returns>Return last day of Month.</returns>
        public static string GetFirstDayInMonth(string monthPartition)
        {
            var monthYear = monthPartition?.Split('_');
            int year = Convert.ToInt16(monthYear.LastOrDefault(), CultureInfo.InvariantCulture);
            int month = Convert.ToInt16(monthYear.FirstOrDefault(), CultureInfo.InvariantCulture);

            DateTime aDateTime = new DateTime(year, month, 1);

            return aDateTime.ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert '/' character of OrgJobPath from Kronos into '$' to save this into Azure table Row/Partition key.
        /// </summary>
        /// <param name="orgJobPath">The actual OrgJobPath which is coming from Kronos.</param>
        /// <returns>Converted OrgJobPath.</returns>
        public static string OrgJobPathDBConversion(string orgJobPath)
        {
            return orgJobPath?.Replace('/', '$');
        }

        /// <summary>
        /// Convert '$' character of OrgJobPath from DB into '/' to compare with Kronos OrgJobPath.
        /// </summary>
        /// <param name="orgJobPath">The OrgJobPath which is coming from DB.</param>
        /// <returns>Converted OrgJobPath.</returns>
        public static string OrgJobPathKronosConversion(string orgJobPath)
        {
            return orgJobPath?.Replace('$', '/');
        }

        /// <summary>
        /// Having the ability to create a new TeamsShiftMappingEntity.
        /// </summary>
        /// <param name="aadUserId">AAD user Id associated with the Shift.</param>
        /// <param name="kronosUniqueId">Kronos Unique Id fro that Shift.</param>
        /// <param name="kronosPersonNumber">Kronos Person number for the user.</param>
        /// <returns>Mapping Entity associated with Team and Shift.</returns>
        public static TeamsShiftMappingEntity CreateShiftMappingEntity(
            string aadUserId,
            string kronosUniqueId,
            string kronosPersonNumber)
        {
            TeamsShiftMappingEntity teamsShiftMappingEntity = new TeamsShiftMappingEntity
            {
                AadUserId = aadUserId,
                KronosUniqueId = kronosUniqueId,
                KronosPersonNumber = kronosPersonNumber,
                ShiftStartDate = DateTime.UtcNow,
            };

            return teamsShiftMappingEntity;
        }

        /// <summary>
        /// Get number of iterations.
        /// </summary>
        /// <param name="processNumberOfUsersInBatch">Number of users to be processed by batch.</param>
        /// <param name="userCount">Kronos user count.</param>
        /// <returns>Number of iterations.</returns>
        public static int GetIterablesCount(int processNumberOfUsersInBatch, int? userCount)
        {
            var iteration = (int)userCount / processNumberOfUsersInBatch;
            iteration = ((int)userCount % processNumberOfUsersInBatch) == 0 ? iteration : iteration + 1;
            return iteration;
        }

        /// <summary>
        /// This method will do the following:
        /// 1. Check the formatting of the dates.
        /// 2. Check whether or not the start date is before the end date.
        /// </summary>
        /// <param name="startDate">The start date passed in as a string.</param>
        /// <param name="endDate">The end date passed in as a string.</param>
        /// <param name="dateFormat">The date formatting.</param>
        /// <returns>A unit of execution that contains a value validating the input information.</returns>
        public static bool CheckDates(
            string startDate,
            string endDate,
            string dateFormat = Common.Constants.DateFormat)
        {
            bool validStartDate = DateTime.TryParseExact(
                startDate,
                dateFormat,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None,
                out var tempStartDate);

            bool validEndDate = DateTime.TryParseExact(
                endDate,
                dateFormat,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None,
                out var tempEndDate);

            return validStartDate && validEndDate && (tempStartDate < tempEndDate);
        }

        /// <summary>
        /// Method that will return the MD5 hash of the shift start date/time timestamp, the end date/time timestamp,
        /// the activities, the notes and the userId.
        /// </summary>
        /// <param name="shift">The shift that has been created from the XML retrieved from Kronos.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>The MD5 string.</returns>
        public string CreateUniqueId(Models.Request.Shift shift, string kronosTimeZone)
        {
            if (shift is null)
            {
                throw new ArgumentNullException(nameof(shift));
            }

            var createUniqueIdProps = new Dictionary<string, string>()
            {
                { "StartDateTimeStamp", shift.SharedShift.StartDateTime.ToString(CultureInfo.InvariantCulture) },
                { "EndDateTimeStamp", shift.SharedShift.EndDateTime.ToString(CultureInfo.InvariantCulture) },
                { "UserId", shift.UserId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var sb = new StringBuilder();

            foreach (var item in shift.SharedShift.Activities)
            {
                sb.Append(item.DisplayName);
                sb.Append(this.CalculateEndDateTime(item.EndDateTime, kronosTimeZone));
                sb.Append(this.CalculateStartDateTime(item.StartDateTime, kronosTimeZone));
            }

            var stringToHash = $"{this.CalculateStartDateTime(shift.SharedShift.StartDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}-{this.CalculateEndDateTime(shift.SharedShift.EndDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}{sb}{shift.SharedShift.Notes}{shift.UserId}";

            this.telemetryClient.TrackTrace($"String to create hash - Shift: {stringToHash}");

            // Utilizing the MD5 hash
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                // Compute the hash from the stringToHash text.
                md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Have the hash result in the byte array
                byte[] hashResult = md5.Hash;

                // Having the actual Hash.
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < hashResult.Length; i++)
                {
                    // Changing the result into 2 hexadecimal digits for each byte in the byte array.
                    strBuilder.Append(hashResult[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var outputHashResult = strBuilder.ToString();

                createUniqueIdProps.Add("ResultingHash", outputHashResult);

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, createUniqueIdProps);

                return outputHashResult;
            }
        }

        /// <summary>
        /// Having the method to construct the notes properly.
        /// </summary>
        /// <param name="shift">The shift that contains the notes.</param>
        /// <returns>A string.</returns>
        public string GetShiftNotes(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ScheduleShift shift)
        {
            string result, noteContents;
            this.telemetryClient.TrackTrace($"GetShiftNotes start at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            if (shift is null)
            {
                throw new ArgumentNullException(nameof(shift));
            }

            if (shift.ShiftComments != null)
            {
                noteContents = string.Empty;
                var notesStr = string.Empty;
                foreach (var item in shift.ShiftComments.Comment)
                {
                    foreach (var noteItem in item?.Notes)
                    {
                        notesStr += noteItem?.Note?.Text + ' ';
                    }

                    noteContents += notesStr + Environment.NewLine;
                }

                result = noteContents;
                this.telemetryClient.TrackTrace($"Notes-ShiftEntity: {result}");
            }
            else
            {
                result = string.Empty;
                var noNotesStr = "There are no notes for this shift";
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                this.telemetryClient.TrackTrace($"Notes-ShiftEntity: {noNotesStr}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            this.telemetryClient.TrackTrace($"GetShiftNotes end at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return result;
        }

        /// <summary>
        /// Method that gets the notes for an open shift.
        /// </summary>
        /// <param name="openShift">The open shift entity from Kronos.</param>
        /// <returns>A string.</returns>
        public string GetOpenShiftNotes(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ScheduleShift openShift)
        {
            var provider = CultureInfo.InvariantCulture;
            string result, noteContents;
            this.telemetryClient.TrackTrace($"GetOpenShiftNotes start at {DateTime.UtcNow.ToString("O", provider)}");

            if (openShift is null)
            {
                throw new ArgumentNullException(nameof(openShift));
            }

            if (openShift.OpenShiftComments != null)
            {
                noteContents = string.Empty;
                var notesStr = string.Empty;
                foreach (var item in openShift.OpenShiftComments.Comment)
                {
                    foreach (var noteItem in item?.Notes)
                    {
                        notesStr += noteItem?.Note?.Text + ' ';
                    }

                    noteContents += notesStr + Environment.NewLine;
                }

                result = noteContents;
                this.telemetryClient.TrackTrace($"Notes-OpenShiftEntity: {result}");
            }
            else
            {
                result = string.Empty;
                this.telemetryClient.TrackTrace("Notes-OpenShiftEntity: There are no notes for this Open Shift");
            }

            this.telemetryClient.TrackTrace($"GetOpenShiftNotes end at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return result;
        }

        /// <summary>
        /// This method creates the expected shift hash using the open shift details.
        /// </summary>
        /// <param name="openShift">The open shift from Graph.</param>
        /// <param name="userAadObjectId">The AAD Object ID of the user.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>A string that represents the expected hash of the new shift that is to be created.</returns>
        public string CreateUniqueId(
            Models.Response.OpenShifts.GraphOpenShift openShift,
            string userAadObjectId,
            string kronosTimeZone)
        {
            if (openShift is null)
            {
                throw new ArgumentNullException(nameof(openShift));
            }

            var createUniqueIdProps = new Dictionary<string, string>()
            {
                { "StartDateTimeStamp", openShift.SharedOpenShift.StartDateTime.ToString(CultureInfo.InvariantCulture) },
                { "EndDateTimeStamp", openShift.SharedOpenShift.EndDateTime.ToString(CultureInfo.InvariantCulture) },
                { "UserId", userAadObjectId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var sb = new StringBuilder();

            foreach (var item in openShift.SharedOpenShift.Activities)
            {
                sb.Append(item.DisplayName);
                sb.Append(this.CalculateEndDateTime(item.EndDateTime, kronosTimeZone));
                sb.Append(this.CalculateStartDateTime(item.StartDateTime, kronosTimeZone));
            }

            var stringToHash = $"{this.CalculateStartDateTime(openShift.SharedOpenShift.StartDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}-{this.CalculateEndDateTime(openShift.SharedOpenShift.EndDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}{sb}{openShift.SharedOpenShift.Notes}{userAadObjectId}";

            this.telemetryClient.TrackTrace($"String to create hash - OpenShift: {stringToHash}");

            // Utilizing the MD5 hash
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                // Compute the hash from the stringToHash text.
                md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Have the hash result in the byte array
                byte[] hashResult = md5.Hash;

                // Having the actual Hash.
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < hashResult.Length; i++)
                {
                    // Changing the result into 2 hexadecimal digits for each byte in the byte array.
                    strBuilder.Append(hashResult[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var outputHashResult = strBuilder.ToString();

                createUniqueIdProps.Add("CreateUniqueId-ExpectedShiftHash", outputHashResult);

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, createUniqueIdProps);

                return outputHashResult;
            }
        }

        /// <summary>
        /// This method creates the expected shift hash using the open shift details.
        /// </summary>
        /// <param name="openShift">The open shift from Graph.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <param name="orgJobPath">The org job path of the open shift.</param>
        /// <returns>A string that represents the expected hash of the new shift that is to be created.</returns>
        public string CreateUniqueId(OpenShiftIS openShift, string kronosTimeZone, string orgJobPath)
        {
            if (openShift is null)
            {
                throw new ArgumentNullException(nameof(openShift));
            }

            var createUniqueIdProps = new Dictionary<string, string>()
            {
                { "StartDateTimeStamp", openShift.SharedOpenShift.StartDateTime.ToString(CultureInfo.InvariantCulture) },
                { "EndDateTimeStamp", openShift.SharedOpenShift.EndDateTime.ToString(CultureInfo.InvariantCulture) },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var sb = new StringBuilder();

            foreach (var item in openShift.SharedOpenShift.Activities)
            {
                sb.Append(item.DisplayName);
                sb.Append(this.CalculateEndDateTime(item.EndDateTime, kronosTimeZone));
                sb.Append(this.CalculateStartDateTime(item.StartDateTime, kronosTimeZone));
            }

            var stringToHash = $"{this.CalculateStartDateTime(openShift.SharedOpenShift.StartDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}-{this.CalculateEndDateTime(openShift.SharedOpenShift.EndDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}{sb}{openShift.SharedOpenShift.Notes}{orgJobPath}";

            this.telemetryClient.TrackTrace($"String to create hash - OpenShift: {stringToHash}");

            // Utilizing the MD5 hash
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                // Compute the hash from the stringToHash text.
                md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Have the hash result in the byte array
                byte[] hashResult = md5.Hash;

                // Having the actual Hash.
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < hashResult.Length; i++)
                {
                    // Changing the result into 2 hexadecimal digits for each byte in the byte array.
                    strBuilder.Append(hashResult[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var outputHashResult = strBuilder.ToString();

                createUniqueIdProps.Add("CreateUniqueId-ExpectedShiftHash", outputHashResult);

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, createUniqueIdProps);

                return outputHashResult;
            }
        }

        /// <summary>
        /// This method generates the Kronos Unique Id for the Shift Entity.
        /// </summary>
        /// <param name="shift">The shift entity that is coming from the response.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>A string that represents the Kronos Unique Id.</returns>
        public string CreateUniqueId(Models.IntegrationAPI.Shift shift, string kronosTimeZone)
        {
            if (shift is null)
            {
                throw new ArgumentNullException(nameof(shift));
            }

            var activities = shift.SharedShift?.Activities ?? shift.DraftShift?.Activities;
            var shiftStartDateTime = (DateTime)(shift.SharedShift?.StartDateTime ?? shift.DraftShift?.StartDateTime);
            var shiftEndDateTime = (DateTime)(shift.SharedShift?.EndDateTime ?? shift.DraftShift?.EndDateTime);
            var notes = shift.SharedShift?.Notes ?? shift.DraftShift?.Notes ?? string.Empty;
            var createUniqueIdProps = new Dictionary<string, string>()
            {
                { "StartDateTimeStamp", shiftStartDateTime.ToString(CultureInfo.InvariantCulture) },
                { "EndDateTimeStamp", shiftEndDateTime.ToString(CultureInfo.InvariantCulture) },
                { "UserId", shift.UserId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "ExecutingAssembly", Assembly.GetExecutingAssembly().GetName().Name },
            };

            var sb = new StringBuilder();

            foreach (var item in activities)
            {
                sb.Append(item.DisplayName);
                sb.Append(this.CalculateEndDateTime(item.EndDateTime, kronosTimeZone));
                sb.Append(this.CalculateStartDateTime(item.StartDateTime, kronosTimeZone));
            }

            // From Kronos to Shifts sync, the notes are passed as an empty string.
            // Therefore, the notes are marked as empty while creating the unique ID from Shifts to Kronos.
            notes = string.Empty;
            var stringToHash = $"{this.CalculateStartDateTime(shiftStartDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}-{this.CalculateEndDateTime(shiftEndDateTime, kronosTimeZone).ToString(CultureInfo.InvariantCulture)}{sb}{notes}{shift.UserId}";

            this.telemetryClient.TrackTrace($"String to create hash - Shift (IntegrationAPI Model): {stringToHash}");

            // Utilizing the MD5 hash
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                // Compute the hash from the stringToHash text.
                md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Have the hash result in the byte array
                byte[] hashResult = md5.Hash;

                // Having the actual hash.
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < hashResult.Length; i++)
                {
                    // Changing the result into 2 hexadecimal digits for each byte in the byte array.
                    strBuilder.Append(hashResult[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var outputHashResult = strBuilder.ToString();

                createUniqueIdProps.Add("ResultingHash", outputHashResult);

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, createUniqueIdProps);

                return outputHashResult;
            }
        }

        /// <summary>
        /// Method overload for the OpenShift entity.
        /// </summary>
        /// <param name="shift">The OpenShift entity.</param>
        /// <param name="orgJobMapping">The org job mapping.</param>
        /// <returns>A string that is the Unique ID of the open shift.</returns>
        public string CreateUniqueId(OpenShift.OpenShiftRequestModel shift, TeamToDepartmentJobMappingEntity orgJobMapping)
        {
            if (shift is null)
            {
                throw new ArgumentNullException(nameof(shift));
            }

            if (orgJobMapping is null)
            {
                throw new ArgumentNullException(nameof(orgJobMapping));
            }

            var createUniqueIdProps = new Dictionary<string, string>()
            {
                { "StartDateTimeStamp", shift.SharedOpenShift.StartDateTime.ToString(CultureInfo.InvariantCulture) },
                { "EndDateTimeStamp", shift.SharedOpenShift.EndDateTime.ToString(CultureInfo.InvariantCulture) },
                { "SchedulingGroupId", shift.SchedulingGroupId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            var sb = new StringBuilder();

            foreach (var item in shift.SharedOpenShift.Activities)
            {
                sb.Append(item.DisplayName);
                sb.Append(this.CalculateEndDateTime(item.EndDateTime, orgJobMapping.KronosTimeZone));
                sb.Append(this.CalculateStartDateTime(item.StartDateTime, orgJobMapping.KronosTimeZone));
            }

            var stringToHash = $"{this.CalculateStartDateTime(shift.SharedOpenShift.StartDateTime, orgJobMapping.KronosTimeZone).ToString(CultureInfo.InvariantCulture)}-{this.CalculateEndDateTime(shift.SharedOpenShift.EndDateTime, orgJobMapping.KronosTimeZone).ToString(CultureInfo.InvariantCulture)}{sb}{shift.SharedOpenShift.Notes}{orgJobMapping.RowKey}";

            this.telemetryClient.TrackTrace($"String to create hash - OpenShiftRequestModel: {stringToHash}");

            // Utilizing the MD5 hash
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                // Compute the hash from the stringToHash text.
                md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                // Have the hash result in the byte array
                byte[] hashResult = md5.Hash;

                // Having the actual Hash.
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < hashResult.Length; i++)
                {
                    // Changing the result into 2 hexadecimal digits for each byte in the byte array.
                    strBuilder.Append(hashResult[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var outputHashResult = strBuilder.ToString();

                createUniqueIdProps.Add("ResultingHash", outputHashResult);

                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, createUniqueIdProps);

                return outputHashResult;
            }
        }

        /// <summary>
        /// Method to properly calculate the StartDateTime in the context of the time off.
        /// </summary>
        /// <param name="requestItem">An item that is type of <see cref="GlobalTimeOffRequestItem"/> that contains the start and end dates.</param>
        /// <param name="kronosTimeZone">The time zone representing the Kronos time zone.</param>
        /// <returns>The type <see cref="DateTimeOffset"/> representing the right start time.</returns>
        public DateTime CalculateStartDateTime(GlobalTimeOffRequestItem requestItem, string kronosTimeZone)
        {
            if (requestItem is null)
            {
                throw new ArgumentNullException(nameof(requestItem));
            }

            // Step 1: Take the raw Kronos date time, and convert it to the Kronos date time zone.
            // Step 2: Convert the Kronos date time to UTC.
            // Step 3: Convert the UTC time to Shifts Time Zone.
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZone);
            var incomingKronosDate = requestItem.TimeOffPeriods.TimeOffPeriod.StartDate;

            string startTimeString;
            DateTime dateTimeStr, utcTime;

            if (requestItem.TimeOffPeriods.TimeOffPeriod.Duration == Resource.DurationInHour)
            {
                // If the duration of the time-off is set for a number of hours.
                startTimeString = requestItem.TimeOffPeriods.TimeOffPeriod.StartTime.ToString(CultureInfo.InvariantCulture);
                dateTimeStr = DateTime.Parse($"{incomingKronosDate} {startTimeString}", CultureInfo.InvariantCulture);
                utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTimeStr, kronosTimeZoneInfo);
            }
            else if (requestItem.TimeOffPeriods.TimeOffPeriod.Duration == Resource.DurationInHalfDay)
            {
                dateTimeStr = DateTime.ParseExact($"{incomingKronosDate} 12:00 PM", "M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);
                utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTimeStr, kronosTimeZoneInfo);
            }
            else
            {
                dateTimeStr = DateTime.ParseExact(incomingKronosDate, "M/dd/yyyy", CultureInfo.InvariantCulture);
                utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTimeStr, kronosTimeZoneInfo);
            }

            return utcTime;
        }

        /// <summary>
        /// This method calculates the start date/time for the open shift entity.
        /// </summary>
        /// <param name="activity">The open shift activity.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start date to utc</param>
        /// <returns>The start date/time to be rendered in the time zone that Shifts has been set up in.</returns>
        public DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var incomingKronosDate = activity.StartDate;
            var incomingKronosTime = activity.StartTime;

            return this.CalculateUtcDateTime(incomingKronosDate, incomingKronosTime, kronosTimeZone);
        }

        /// <summary>
        /// Method to calculate the start date time for the shift segment in the batch open shift request.
        /// </summary>
        /// <param name="activity">The shift segment which is part of the batch.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start date to utc</param>
        /// <returns>A date-time offset.</returns>
        public DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return this.CalculateUtcDateTime(activity.StartDate, activity.StartTime, kronosTimeZone);
        }

        /// <summary>
        /// Method to calculate the end date time for the shift segment in the batch open shift request.
        /// </summary>
        /// <param name="activity">The shift segment which is part of the batch.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start date to utc.</param>
        /// <returns>A date-time offset.</returns>
        public DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return this.CalculateUtcDateTime(activity.EndDate, activity.EndTime, kronosTimeZone);
        }

        /// <summary>
        /// Method that will calculate the end date time.
        /// </summary>
        /// <param name="endDateTime">The incoming end date time object.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the end datetime.</param>
        /// <returns>A date time result.</returns>
        public DateTime CalculateEndDateTime(DateTime endDateTime, string kronosTimeZone)
        {
            return this.CalculateLocalDateTime(endDateTime, kronosTimeZone);
        }

        /// <summary>
        /// Method that calculates the start date time.
        /// </summary>
        /// <param name="startDateTime">The incoming start date time object.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start datetime.</param>
        /// <returns>A date time result.</returns>
        public DateTime CalculateStartDateTime(DateTime startDateTime, string kronosTimeZone)
        {
            return this.CalculateLocalDateTime(startDateTime, kronosTimeZone);
        }

        /// <summary>
        /// This method calculates from "UTC" to the expected time.
        /// </summary>
        /// <param name="startDateTime">Incoming start date time.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start datetime.</param>
        /// <returns>A date time object.</returns>
        public DateTime CalculateStartDateTime(DateTimeOffset startDateTime, string kronosTimeZone)
        {
            var incomingKronosDate = startDateTime.ToString("d", CultureInfo.InvariantCulture);
            var incomingKronosTime = startDateTime.ToString("t", CultureInfo.InvariantCulture);

            return this.CalculateLocalDateTime(incomingKronosDate, incomingKronosTime, kronosTimeZone);
        }

        /// <summary>
        /// This method calculates from "UTC" to the expected time.
        /// </summary>
        /// <param name="endDateTime">Incoming end date time.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start datetime.</param>
        /// <returns>A date time object.</returns>
        public DateTime CalculateEndDateTime(DateTimeOffset endDateTime, string kronosTimeZone)
        {
            var incomingKronosDate = endDateTime.ToString("d", CultureInfo.InvariantCulture);
            var incomingKronosTime = endDateTime.ToString("t", CultureInfo.InvariantCulture);

            return this.CalculateLocalDateTime(incomingKronosDate, incomingKronosTime, kronosTimeZone);
        }

        /// <summary>
        /// This method calculates the end date/time for the open shift entity.
        /// </summary>
        /// <param name="activity">The open shift activity.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the end datetime.</param>
        /// <returns>The end date/time to be rendered in the time zone that Shifts has been set up in.</returns>
        public DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return this.CalculateUtcDateTime(activity.EndDate, activity.EndTime, kronosTimeZone);
        }

        /// <summary>
        /// This method calculates the start date/time for the shift entity.
        /// </summary>
        /// <param name="activity">The shift activity.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the start date to utc.</param>
        /// <returns>The start date/time to be rendered in the time zone that Shifts has been set up in.</returns>
        public DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return this.CalculateUtcDateTime(activity.StartDate, activity.StartTime, kronosTimeZone);
        }

        /// <summary>
        /// This method calculates the end date/time for the shift entity.
        /// </summary>
        /// <param name="activity">The shift activity.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the end date to utc.</param>
        /// <returns>The end date/time to be rendered in the time zone that Shifts has been set up in.</returns>
        public DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ShiftSegment activity, string kronosTimeZone)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return this.CalculateUtcDateTime(activity.EndDate, activity.EndTime, kronosTimeZone);
        }

        /// <summary>
        /// This method converts UTC time to Kronos time zone.
        /// </summary>
        /// <param name="dateTimeOffset">UTC nullable date time offset.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting from UTC to Kronos time.</param>
        /// <returns>Kronos date time.</returns>
        public DateTime UTCToKronosTimeZone(DateTimeOffset? dateTimeOffset, string kronosTimeZone)
        {
            var kronosTimeZoneId = string.IsNullOrEmpty(kronosTimeZone) ? this.appSettings.KronosTimeZone : kronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(
                dateTimeOffset != null ?
                dateTimeOffset.GetValueOrDefault().DateTime
                : DateTime.MaxValue, kronosTimeZoneInfo);
        }

        /// <summary>
        /// This method converts UTC time to Kronos time zone.
        /// </summary>
        /// <param name="dateTime">Date time.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting from UTC to Kronos time.</param>
        /// <returns>Kronos date time.</returns>
        public DateTime UTCToKronosTimeZone(DateTime dateTime, string kronosTimeZone)
        {
            var kronosTimeZoneId = string.IsNullOrEmpty(kronosTimeZone) ? this.appSettings.KronosTimeZone : kronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, kronosTimeZoneInfo);
        }

        /// <summary>
        /// Having the ability to create a new TeamsShiftMappingEntity.
        /// </summary>
        /// <param name="shift">The Shift model.</param>
        /// <param name="userMappingEntity">Details of user from User Mapping Entity table.</param>
        /// <param name="kronosUniqueId">Kronos Unique Id corresponds to the shift.</param>
        /// <returns>Mapping Entity associated with Team and Shift.</returns>
        public TeamsShiftMappingEntity CreateShiftMappingEntity(
           Models.IntegrationAPI.Shift shift,
           AllUserMappingEntity userMappingEntity,
           string kronosUniqueId)
        {
            var startDateTime = DateTime.SpecifyKind(shift.SharedShift.StartDateTime, DateTimeKind.Utc);

            return new TeamsShiftMappingEntity
            {
                AadUserId = shift?.UserId,
                KronosUniqueId = kronosUniqueId,
                KronosPersonNumber = userMappingEntity?.RowKey,
                ShiftStartDate = startDateTime,
            };
        }

        /// <summary>
        /// This method will establish a Kronos session (Jsession).
        /// </summary>
        /// <returns>The string that represents the Jsession for Kronos.</returns>
        public async Task<string> GetKronosSessionAsync()
        {
            Logon.Response loginKronosResult;

            loginKronosResult = JsonConvert.DeserializeObject<Logon.Response>(
            await this.cache.GetStringAsync(Common.Constants.KronosLoginCacheKey).ConfigureAwait(false) ?? string.Empty);

            if (loginKronosResult == null && string.IsNullOrEmpty(loginKronosResult?.Jsession))
            {
                var configurationEntity = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false)).FirstOrDefault();

                var loginKronos = this.logonActivity.LogonAsync(
                 this.appSettings.WfmSuperUsername,
                 this.appSettings.WfmSuperUserPassword,
                 new Uri(configurationEntity?.WfmApiEndpoint));

                loginKronosResult = await loginKronos.ConfigureAwait(false);

                if (loginKronosResult is null)
                {
                    this.telemetryClient.TrackTrace($"Login Kronos Data Object: {loginKronosResult}");
                }

                await this.cache.SetStringAsync(
                    Common.Constants.KronosLoginCacheKey,
                    JsonConvert.SerializeObject(loginKronosResult),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Constants.KronosCacheTimeOut),
                    }).ConfigureAwait(false);
            }

            return loginKronosResult?.Jsession;
        }

        /// <summary>
        /// Method to check whether or not all of the prerequisites are done.
        /// </summary>
        /// <returns>A value indicating whether or not the required setup is done.</returns>
        public async Task<bool> IsSetUpDoneAsync()
        {
            bool isSetupDone = false;
            var configurationEntity = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false)).FirstOrDefault();
            if (!string.IsNullOrEmpty(configurationEntity?.WorkforceIntegrationId))
            {
                var teamDeptRecords = await this.azureTableStorageHelper.FetchTableRecordsAsync<TeamToDepartmentJobMappingEntity>(this.appSettings.TeamDepartmentMapping, configurationEntity?.WorkforceIntegrationId).ConfigureAwait(false);
                var userRecords = await this.azureTableStorageHelper.FetchTableRecordsAsync<AllUserMappingEntity>(this.appSettings.UserToUserMapping, null).ConfigureAwait(false);

                isSetupDone = userRecords?.Count > 0 && teamDeptRecords?.Count > 0;
            }

            string isSetupDoneMessage = string.Format(CultureInfo.InvariantCulture, Resource.SetupStatusMessage, isSetupDone);
            this.telemetryClient.TrackTrace(isSetupDoneMessage);

            return isSetupDone;
        }

        /// <summary>
        /// This method will be able to get all of the necessary prerequisites.
        /// </summary>
        /// <returns>A unit of execution that contains an object of type <see cref="SetupDetails"/>.</returns>
        public async Task<SetupDetails> GetAllConfigurationsAsync()
        {
            SetupDetails setupDetails = new SetupDetails();

            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CallingMethod", "UpdateTeam" },
            };

            this.GetTenantDetails(out string tenantId, out string clientId, out string clientSecret, out string instance);

            // Get configuration info from table.
            var configurationEntity = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false)).FirstOrDefault();

            if (!string.IsNullOrEmpty(configurationEntity?.WfmApiEndpoint))
            {
                setupDetails.WfmEndPoint = configurationEntity?.WfmApiEndpoint;
            }
            else
            {
                setupDetails.ErrorMessage += Resource.KronosURLNotPresent;
            }

            if (!string.IsNullOrEmpty(configurationEntity?.AdminAadObjectId))
            {
                setupDetails.WFIId = configurationEntity?.WorkforceIntegrationId;
                var accessToken = this.graphUtility.GetAccessTokenAsync(tenantId, instance, clientId, clientSecret, configurationEntity?.AdminAadObjectId).GetAwaiter().GetResult();

                setupDetails.ShiftsAdminAadObjectId = configurationEntity?.AdminAadObjectId;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    setupDetails.ShiftsAccessToken = accessToken;
                    setupDetails.TenantId = tenantId;
                }
                else
                {
                    telemetryProps.Add("ShiftAccessToken", Resource.IssueShiftsAccessToken);
                    setupDetails.ErrorMessage += Resource.IssueShiftsAccessToken;
                }

                setupDetails.KronosSession = this.GetKronosSessionAsync().GetAwaiter().GetResult();
                setupDetails.KronosUserName = this.appSettings.WfmSuperUsername;
                setupDetails.KronosPassword = this.appSettings.WfmSuperUserPassword;

                if (string.IsNullOrEmpty(setupDetails.KronosSession))
                {
                    telemetryProps.Add("KronosStatus", Resource.InvalidKronosCredentials);
                    setupDetails.ErrorMessage += Resource.InvalidKronosCredentials;
                }
            }
            else
            {
                setupDetails.ErrorMessage += Resource.WorkforceIntegrationAdminNotFound;
                telemetryProps.Add("WorkforceIntegrationStatus", Resource.WorkforceIntegrationAdminNotFound);
            }

            setupDetails.IsAllSetUpExists =
                !string.IsNullOrEmpty(setupDetails.WFIId)
                && !string.IsNullOrEmpty(setupDetails.ShiftsAccessToken)
                && !string.IsNullOrEmpty(setupDetails.KronosSession)
                && !string.IsNullOrEmpty(setupDetails.WfmEndPoint)
                && !string.IsNullOrEmpty(setupDetails.KronosPassword)
                && !string.IsNullOrEmpty(setupDetails.KronosUserName);

            return setupDetails;
        }

        /// <summary>
        /// Set the query date span based on incoming request.
        /// </summary>
        /// <param name="isNotFirstTimeSync">true if request is coming any time after the initial sync, else false.</param>
        /// <param name="startDate">returns start date.</param>
        /// <param name="endDate">returns end date.</param>
        public void SetQuerySpan(bool isNotFirstTimeSync, out string startDate, out string endDate)
        {
            if (isNotFirstTimeSync)
            {
                var currentDate = DateTime.UtcNow;
                startDate = currentDate.AddDays(-Convert.ToDouble(this.appSettings.SyncFromPreviousDays, CultureInfo.InvariantCulture)).ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
                endDate = currentDate.AddDays(Convert.ToDouble(this.appSettings.SyncToNextDays, CultureInfo.InvariantCulture)).ToString("M/dd/yyyy", CultureInfo.InvariantCulture);
            }
            else
            {
                startDate = this.appSettings.ShiftStartDate;
                endDate = this.appSettings.ShiftEndDate;
            }
        }

        /// <summary>
        /// Converts a given datetime into correct format for Kronos calls.
        /// </summary>
        /// <param name="date">A <see cref="DateTime"/>.</param>
        /// <returns>A string representation of the date for a kronos call.</returns>
        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "This format is needed for kronos calls.")]
        public string ConvertToKronosDate(DateTime date) => date.ToString(this.appSettings.KronosQueryDateSpanFormat);

        /// <summary>
        /// Method to retrieve the necessary details from AppSettings.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="instance">The instance URL.</param>
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
        /// Method converting the specified date and time to a single DateTime in UTC.
        /// </summary>
        /// <param name="datePart">The date part to convert.</param>
        /// <param name="timePart">The time part to convert.</param>
        /// <param name="kronosTimeZone">The time zone the date and time are expressed in.</param>
        /// <returns>The UTC datetime.</returns>
        private DateTime CalculateUtcDateTime(string datePart, string timePart, string kronosTimeZone)
        {
            var kronosTimeZoneId = string.IsNullOrEmpty(kronosTimeZone) ? this.appSettings.KronosTimeZone : kronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            DateTime dateTimeStr = DateTime.Parse($"{datePart} {timePart}", CultureInfo.InvariantCulture);
            return TimeZoneInfo.ConvertTimeToUtc(dateTimeStr, kronosTimeZoneInfo);
        }

        /// <summary>
        /// Method converting the specified datetime expressed in UTC to a datetime expressed in the specified time zone.
        /// </summary>
        /// <param name="dateTime">The UTC datetime to convert.</param>
        /// <param name="kronosTimeZone">The time zone to convert the time to.</param>
        /// <returns>The converted datetime.</returns>
        private DateTime CalculateLocalDateTime(DateTime dateTime, string kronosTimeZone)
        {
            var incomingKronosDate = dateTime.ToString("d", CultureInfo.InvariantCulture);
            var incomingKronosTime = dateTime.ToString("t", CultureInfo.InvariantCulture);

            return this.CalculateLocalDateTime(incomingKronosDate, incomingKronosTime, kronosTimeZone);
        }

        /// <summary>
        /// Method converting the specified date and time in UTC to a single DateTime in the specified time zone.
        /// </summary>
        /// <param name="datePart">The date part to convert.</param>
        /// <param name="timePart">The time part to convert.</param>
        /// <param name="kronosTimeZone">The time zone the date and time are to be converted to.</param>
        /// <returns>The converted datetime.</returns>
        private DateTime CalculateLocalDateTime(string datePart, string timePart, string kronosTimeZone)
        {
            var kronosTimeZoneId = string.IsNullOrEmpty(kronosTimeZone) ? this.appSettings.KronosTimeZone : kronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            DateTime dateTimeStr = DateTime.Parse($"{datePart} {timePart}", CultureInfo.InvariantCulture);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTimeStr, kronosTimeZoneInfo);
        }
    }
}