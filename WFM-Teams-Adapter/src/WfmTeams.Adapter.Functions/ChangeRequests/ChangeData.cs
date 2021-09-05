// ---------------------------------------------------------------------------
// <copyright file="ChangeData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.ChangeRequests
{
    using System.Collections.Generic;

    public class ChangeData
    {
        public enum RequestStatus
        {
            NotStarted,
            InProgress,
            Complete
        }

        public ChangeResultModel ManagerResult { get; set; }
        public RequestStatus ManagerStatus { get; set; } = RequestStatus.NotStarted;
        public ChangeResultModel RecipientResult { get; set; }
        public RequestStatus RecipientStatus { get; set; } = RequestStatus.NotStarted;
        public ChangeResultModel SenderResult { get; set; }
        public RequestStatus SenderStatus { get; set; } = RequestStatus.NotStarted;
        public List<string> ShiftIds { get; set; }
        public string SwapRequestId { get; set; }
    }
}
