// ---------------------------------------------------------------------------
// <copyright file="WfmError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    public class WfmError
    {
        public const string UnspecfiedErrorCode = "Unspecified";

        public string Code { get; set; } = UnspecfiedErrorCode;

        public string Message { get; set; }
    }
}
