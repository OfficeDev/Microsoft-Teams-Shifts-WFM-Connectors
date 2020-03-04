// <copyright file="DataTableViewModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    using System.Collections.Generic;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This class will model out the data for the table.
    /// </summary>
    public class DataTableViewModel
    {
        /// <summary>
        /// Gets the list of configurations that are saved by the configuration web app.
        /// </summary>
        public List<ConfigurationEntity> ConfigurationsList { get; }
    }
}