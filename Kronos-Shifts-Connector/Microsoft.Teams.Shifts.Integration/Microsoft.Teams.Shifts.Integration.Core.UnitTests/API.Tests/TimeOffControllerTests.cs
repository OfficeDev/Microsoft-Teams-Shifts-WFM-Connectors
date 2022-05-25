using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Microsoft.Teams.Shifts.Integration.Core.UnitTests.Common;
using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
using System;
using Xunit;
using NSubstitute;
using Microsoft.Teams.Shifts.Integration.API.Common;
using Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;
using Microsoft.Teams.Shifts.Integration.API.Controllers;
using Microsoft.ApplicationInsights;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
using Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff;
using System.Net.Http;
using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common;
using System.Threading.Tasks;

namespace Microsoft.Teams.Shifts.Integration.Core.UnitTests.API.Tests
{
    public class TimeOffControllerTests
    {
        // Date Format:  yyyy/mm/dd
        [Theory]
        [InlineAutoNSubstituteData(true, "Success", "Pacific Standard Time", true, "")]
        [InlineAutoNSubstituteData(false, "Success", "Pacific Standard Time", true, "")]
        [InlineAutoNSubstituteData(true, "Failure", "Pacific Standard Time", false, "36015")]
        public async void ApproveOrDenyTimeOffRequestInKronos_ApprovedSuccessful_DeniedSuccessful_ApprovedFailure
        (
            bool isApproved,
            string status,
            string kronosTimeZone,
            bool expected,
            string errorCode,            
            string kronosReqId,
            string kronosUserId,
            TimeOffRequestItem teamsTimeOffEntity,
            TimeOffMappingEntity timeOffRequestMapping,
            string managerMessage,
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IUserMappingProvider userMappingProvider,
            ITimeOffActivity timeOffActivity,
            ITimeOffReasonProvider timeOffReasonProvider,
            IAzureTableStorageHelper azureTableStorageHelper,
            ITimeOffMappingEntityProvider timeOffMappingEntityProvider,
            IUtility utility,
            IGraphUtility graphUtility,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IHttpClientFactory httpClientFactory,
            BackgroundTaskWrapper taskWrapper,
            SetupDetails setup
            
        )
        {
            //  Arrange
            setup.IsAllSetUpExists = true;
            setup.WfmEndPoint = "http://microsoft.com/";
            timeOffRequestMapping.PayCodeName = "Vacation";

            utility.GetAllConfigurationsAsync()
                .Returns(setup);
            timeOffActivity.ApproveOrDenyTimeOffRequestAsync(
                Arg.Any<Uri>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<Comments>())
                    .Returns(getResponse(status));

            timeOffMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(
                Arg.Any<TimeOffMappingEntity>())
                    .Returns(r => Task<Response>
                    .FromResult(r));

            TimeOffController timeOffController = new TimeOffController(
                appSettings,
                telemetryClient,
                userMappingProvider,
                timeOffActivity,
                timeOffReasonProvider,
                azureTableStorageHelper,
                timeOffMappingEntityProvider,
                utility,
                graphUtility,
                teamDepartmentMappingProvider,
                httpClientFactory,
                taskWrapper);


            //  Act
            var result = timeOffController.ApproveOrDenyTimeOffRequestInKronos(
                kronosReqId,
                kronosUserId,
                teamsTimeOffEntity,
                timeOffRequestMapping,
                managerMessage,
                isApproved,
                kronosTimeZone
            );

            //  Assert
            if (expected)
                Assert.Equal(expected, result.Result);
            else
            {
                Func<Task> act = () => timeOffController.ApproveOrDenyTimeOffRequestInKronos(
                        kronosReqId,
                        kronosUserId,
                        teamsTimeOffEntity,
                        timeOffRequestMapping,
                        managerMessage,
                        isApproved,
                        kronosTimeZone
                    );

                var exception = await Assert.ThrowsAsync<Exception>(act);
                Assert.Contains(errorCode, exception.Message, StringComparison.OrdinalIgnoreCase);
            }           
        }

        private Response getResponse(string status)
        {
            Response response = null;

            if (status == "Success")
            {
                response = new Response
                {
                    Status = status
                };
            }
            else
            {
                response = new Response
                {
                    Status = status,
                    Error = new App.KronosWfc.Models.ResponseEntities.Error
                    {
                        DetailErrors = new App.KronosWfc.Models.ResponseEntities.ErrorArr
                        {
                            Error = new App.KronosWfc.Models.ResponseEntities.Error[]
                               {
                                    new App.KronosWfc.Models.ResponseEntities.Error
                                    {
                                        ErrorCode = "36015",
                                        Message = "Vacation balance on 06/03/2022 is 0:00 (overdrawn by 8:00). Maximum overdraw is 0:00.",
                                    }
                               }
                        }
                    }
                };
            }
            return response;
        }
    }
}
