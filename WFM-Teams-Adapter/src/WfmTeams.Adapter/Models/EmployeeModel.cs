// ---------------------------------------------------------------------------
// <copyright file="EmployeeModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines the employee model.
    /// </summary>
    public class EmployeeModel
    {
        public string TeamsEmployeeId { get; set; }
        public string TeamsLoginName { get; set; }
        public string DisplayName { get; set; }
        public bool IsManager { get; set; }
        public string WfmEmployeeId { get; set; }
        public string WfmLoginName { get; set; }
        public List<string> TeamIds { get; set; } = new List<string>();
    }
}
