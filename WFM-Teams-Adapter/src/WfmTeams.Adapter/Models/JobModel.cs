// ---------------------------------------------------------------------------
// <copyright file="JobModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    /// <summary>
    /// Defines a model of a job in the WFM provider.
    /// </summary>
    public class JobModel
    {
        public string DepartmentName { get; set; }
        public string Name { get; set; }
        public string WfmId { get; set; }
        public string ThemeCode { get; set; }
    }
}
