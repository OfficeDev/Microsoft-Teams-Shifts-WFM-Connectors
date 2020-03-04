// <copyright file="KronosUserModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    /// <summary>
    /// This class will model the necessary Kronos's user information.
    /// </summary>
    public class KronosUserModel
    {
        /// <summary>
        /// Gets or sets the org job path in Kronos.
        /// </summary>
        public string KronosOrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the UPN of the User in Shifts.
        /// </summary>
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the User in Kronos.
        /// </summary>
        public string KronosUserName { get; set; }
    }
}