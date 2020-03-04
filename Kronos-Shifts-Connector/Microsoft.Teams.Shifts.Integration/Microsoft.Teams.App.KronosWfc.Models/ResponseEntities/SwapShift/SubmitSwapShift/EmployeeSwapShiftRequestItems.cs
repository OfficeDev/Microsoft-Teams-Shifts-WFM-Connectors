// <copyright file="EmployeeSwapShiftRequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift
{
    /// <summary>
    /// This class modles the EmployeeSwapShiftRequestItems.
    /// </summary>
    public class EmployeeSwapShiftRequestItems
    {
        /// <summary>
        /// Gets or sets the EmployeeSwapShiftRequestItem.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public EmployeeSwapShiftRequestItem[] EmployeeSwapShiftRequestItem { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}