// ---------------------------------------------------------------------------
// <copyright file="IWfmActionService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;

    public interface IWfmActionService
    {
        Task<WfmResponse> CreateShiftSwapRequestAsync(WfmShiftSwapModel swapModel, ILogger log);

        Task<WfmResponse> CancelShiftSwapRequestAsync(WfmShiftSwapModel swapModel, ILogger log);

        Task<WfmResponse> RecipientApproveShiftSwapRequestAsync(WfmShiftSwapModel swapModel, bool approve, ILogger log);

        Task<WfmResponse> ManagerApproveShiftSwapRequestAsync(WfmShiftSwapModel swapModel, bool approve, ILogger log);

        Task<WfmResponse> CreateOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftModel, ILogger log);

        Task<WfmResponse> CancelOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftModel, ILogger log);

        Task<WfmResponse> ManagerApproveOpenShiftRequestAsync(WfmOpenShiftRequestModel openShiftModel, bool approve, ILogger log);

        Task<WfmResponse> ManagerAssignOpenShiftAsync(ShiftModel assignedOpenShift, EmployeeModel manager, EmployeeModel employee, string wfmBuId, string timeZoneInfoId, ILogger log);

        Task<WfmResponse> UpdateEmployeeAvailabilityAsync(EmployeeAvailabilityModel availabilityModel, ILogger log);
    }
}
