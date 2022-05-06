using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Microsoft.Teams.Shifts.Integration.Core.UnitTests.Common;
using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
using System;
using Xunit;

using Microsoft.Teams.Shifts.Integration.API.Common;
//using Microsoft.Teams.Shifts.Integration.

namespace Microsoft.Teams.Shifts.Integration.Core.UnitTests.API.Tests
{
    public class UtilityTests
    {
        // Date Format:  yyyy/mm/dd
        [Theory]
        [InlineAutoNSubstituteData("2022-04-30", "2022-05-01", true)]
        [InlineAutoNSubstituteData("2022-05-01", "2022-04-30", false)]
        public void CreateShiftMappingEntityTest_StartAndEndDatesPresent_EndDateGreaterThanStart
        (
            string startDate,
            string endDate,
            bool expected,
            Utility utility,
            Shift shift,
            AllUserMappingEntity userMappingEntity,
            string kronosUniqueId,
            string teamId
        )
        {
            //  Arrange
            DateTime dtmStart = DateTime.Parse(startDate);
            DateTime dtmEnd = DateTime.Parse(endDate);

            shift.SharedShift.StartDateTime = dtmStart;
            shift.SharedShift.EndDateTime = dtmEnd;

            //  Act
            var actual = utility.CreateShiftMappingEntity(shift, userMappingEntity, kronosUniqueId, teamId);

            //  Assert
            Assert.Equal(dtmStart,actual.ShiftStartDate);  
            Assert.Equal(dtmEnd, actual.ShiftEndDate);
            Assert.Equal(expected, (actual.ShiftStartDate < actual.ShiftEndDate));
        }
    }
}
