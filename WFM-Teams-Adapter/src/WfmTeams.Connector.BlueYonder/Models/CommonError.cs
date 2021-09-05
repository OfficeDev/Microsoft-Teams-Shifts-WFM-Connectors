// ---------------------------------------------------------------------------
// <copyright file="CommonError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Models
{
    using System;

    public class CommonErrorResponse
    {
        public object DevMessage { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorInstanceId { get; set; }
        public string MoreInfo { get; set; }
        public string Timestamp { get; set; }
        public String UserMessage { get; set; }
    }
}
