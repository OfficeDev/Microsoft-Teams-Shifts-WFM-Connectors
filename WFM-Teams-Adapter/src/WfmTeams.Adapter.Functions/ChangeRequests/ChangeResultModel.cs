// ---------------------------------------------------------------------------
// <copyright file="ChangeResultModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.ChangeRequests
{
    public class ChangeResultModel
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public int StatusCode { get; set; }
    }
}