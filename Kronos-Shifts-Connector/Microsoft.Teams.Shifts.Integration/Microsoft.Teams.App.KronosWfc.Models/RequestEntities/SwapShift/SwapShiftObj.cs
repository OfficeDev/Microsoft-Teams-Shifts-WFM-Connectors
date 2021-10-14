// <copyright file="SwapShiftObj.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift
{
    using System;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the SwapShiftObj.
    /// </summary>
    public class SwapShiftObj
    {
        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the RequestorPersonNumber.
        /// </summary>
        public string RequestorPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the RequestedToPersonNumber (Change to RequesteePersonNumber).
        /// </summary>
        public string RequestedToPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the RequestorName.
        /// </summary>
        public string RequestorName { get; set; }

        /// <summary>
        /// Gets or sets the RequestToName (Change to RequesteeName).
        /// </summary>
        public string RequestedToName { get; set; }

        /// <summary>
        /// Gets or sets Emp1FromDateTime.
        /// </summary>
        public DateTime Emp1FromDateTime { get; set; }

        /// <summary>
        /// Gets or sets Emp1ToDateTime.
        /// </summary>
        public DateTime Emp1ToDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Emp2FromDateTime.
        /// </summary>
        public DateTime Emp2FromDateTime { get; set; }

        /// <summary>
        /// Gets or sets Emp2ToDateTime.
        /// </summary>
        public DateTime Emp2ToDateTime { get; set; }

        /// <summary>
        /// Gets or sets the SelectedShiftToSwap.
        /// </summary>
        public string SelectedShiftToSwap { get; set; }

        /// <summary>
        /// Gets or sets the SelectedLocation.
        /// </summary>
        public string SelectedLocation { get; set; }

        /// <summary>
        /// Gets or sets the SelectedJob.
        /// </summary>
        public string SelectedJob { get; set; }

        /// <summary>
        /// Gets or sets the SelectedEmployee.
        /// </summary>
        public string SelectedEmployee { get; set; }

        /// <summary>
        /// Gets or sets the SelectedAvailableShift.
        /// </summary>
        public string SelectedAvailableShift { get; set; }

        /// <summary>
        /// Gets or sets the RequestId.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        public Comments Comments { get; set; }
    }
}