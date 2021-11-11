// ---------------------------------------------------------------------------
// <copyright file="IAvailabilityMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Mappings
{
    using WfmTeams.Adapter.Models;
    using WfmTeams.Connector.BlueYonder.Models;

    public interface IAvailabilityMap
    {
        EmployeeAvailabilityModel MapAvailability(EmployeeAvailabilityCollectionResource availabilityCollection, string timeZoneInfoId);

        EmployeeAvailabilityResource MapAvailability(EmployeeAvailabilityCollectionResource existingAvailability, EmployeeAvailabilityModel availabilityModel);
    }
}
