// ---------------------------------------------------------------------------
// <copyright file="IUserPrincipalMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Mappings
{
    using System.Collections.Generic;
    using WfmTeams.Adapter.Models;

    /// <summary>
    /// Defines the interface of the user principal map.
    /// </summary>
    public interface IUserPrincipalMap
    {
        EmployeeModel MapEmployee(string login, IDictionary<string, EmployeeModel> employees);
    }
}
