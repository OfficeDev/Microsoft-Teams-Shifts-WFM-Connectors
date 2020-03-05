// <copyright file="Employees.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift
{
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule;

    /// <summary>
    /// This class models the employees.
    /// </summary>
    public class Employees
    {
        /// <summary>
        /// Gets or sets the personIdentity.
        /// </summary>
        public PersonIdentity PersonIdentity { get; set; }
    }
}