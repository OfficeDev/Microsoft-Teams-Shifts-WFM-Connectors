// ---------------------------------------------------------------------------
// <copyright file="IAvailabilityMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Mappings
{
    using System.Collections.Generic;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;

    public interface IAvailabilityMap
    {
        IList<AvailabilityItem> MapAvailability(EmployeeAvailabilityModel availabilityModel);

        EmployeeAvailabilityModel MapAvailability(IList<AvailabilityItem> availabilityItems, string userId);
    }
}
