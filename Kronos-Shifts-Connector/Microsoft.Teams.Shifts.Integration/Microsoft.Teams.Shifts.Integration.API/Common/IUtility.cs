// <copyright file="Utility.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
using Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShifts;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.Graph;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift;
using System;
using System.Threading.Tasks;

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    public interface IUtility
    {
        DateTime CalculateEndDateTime(DateTime endDateTime, string kronosTimeZone);
        DateTime CalculateEndDateTime(DateTimeOffset endDateTime, string kronosTimeZone);
        DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ShiftSegment activity, string kronosTimeZone);
        DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment activity, string kronosTimeZone);
        DateTime CalculateEndDateTime(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ShiftSegment activity, string kronosTimeZone);
        DateTime CalculateStartDateTime(DateTime startDateTime, string kronosTimeZone);
        DateTime CalculateStartDateTime(DateTimeOffset startDateTime, string kronosTimeZone);
        DateTime CalculateStartDateTime(GlobalTimeOffRequestItem requestItem, string kronosTimeZone);
        DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment activity, string kronosTimeZone);
        DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ShiftSegment activity, string kronosTimeZone);
        DateTime CalculateStartDateTime(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ShiftSegment activity, string kronosTimeZone);
        string CreateOpenShiftInTeamsUniqueId(OpenShiftIS openShift, string kronosTimeZone, string orgJobPath);
        TeamsShiftMappingEntity CreateShiftMappingEntity(Shift shift, AllUserMappingEntity userMappingEntity, string kronosUniqueId, string teamId);
        string CreateShiftUniqueId(Shift shift, string kronosTimeZone);
        string CreateUniqueId(GraphOpenShift openShift, string userAadObjectId, string kronosTimeZone);
        string CreateUniqueId(OpenShiftRequestModel shift, TeamToDepartmentJobMappingEntity orgJobMapping);
        string CreateUniqueId(Models.Request.Shift shift, string kronosTimeZone);
        string FormatDateForKronos(DateTime date);
        Task<SetupDetails> GetAllConfigurationsAsync();
        Task<string> GetKronosSessionAsync();
        string GetOpenShiftNotes(App.KronosWfc.Models.ResponseEntities.OpenShift.Batch.ScheduleShift openShift);
        string GetShiftNotes(App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.ScheduleShift shift);
        GraphConfigurationDetails GetTenantDetails();
        Task<bool> IsSetUpDoneAsync();
        void SetQuerySpan(bool isNotFirstTimeSync, out string startDate, out string endDate);
        DateTime UTCToKronosTimeZone(DateTime dateTime, string kronosTimeZone);
        DateTime UTCToKronosTimeZone(DateTimeOffset? dateTimeOffset, string kronosTimeZone);
    }
}