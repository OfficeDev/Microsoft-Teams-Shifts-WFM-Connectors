// ---------------------------------------------------------------------------
// <copyright file="OpenShiftsChangeRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.MicrosoftGraph.Enums;
    using WfmTeams.Adapter.Models;

    public partial class OpenShiftsChangeRequest : IHandledRequest
    {
        [JsonIgnore]
        public bool IsApproved => (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase)
            && State.Equals(ChangeRequestPhase.Approved, StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public bool IsManager => (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase)
            && State.Equals(ChangeRequestPhase.Approved, StringComparison.OrdinalIgnoreCase))
            || (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase)
            && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public bool IsSystem => AssignedTo.Equals(ChangeRequestAssignedTo.System, StringComparison.OrdinalIgnoreCase)
            && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase);

        [JsonProperty(PropertyName = "wfmManagerId")]
        public string WfmManagerId { get; set; }

        [JsonProperty(PropertyName = "wfmManagerLoginName")]
        public string WfmManagerLoginName { get; set; }

        [JsonProperty(PropertyName = "wfmOpenShiftId")]
        public string WfmOpenShiftId { get; set; }

        [JsonProperty(PropertyName = "wfmSenderId")]
        public string WfmSenderId { get; set; }

        [JsonProperty(PropertyName = "wfmSenderLoginName")]
        public string WfmSenderLoginName { get; set; }

        public ChangeRequestState EvaluateState(string method)
        {
            if (method.Equals("delete", StringComparison.OrdinalIgnoreCase)
                || (IsSystem && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase)))
            {
                return ChangeRequestState.RequestCancelled;
            }
            else if (IsManager)
            {
                return IsApproved
                    ? ChangeRequestState.ManagerApproved
                    : ChangeRequestState.ManagerDeclined;
            }

            return ChangeRequestState.ManagerPending;
        }

        public string EvaluateTargetLoginName()
        {
            // regardless of whether this is the sender or manager stage of the open shift request
            // process, our test WFM has no concept of manager approval and therefore this method
            // should always return the target login name of the sender TODO : is this valid for
            // other WFM providers?
            return WfmSenderLoginName;
        }

        public string EvaluateUserId()
        {
            // regardless of whether this is the sender or manager stage of the open shift request
            // process, our test WFM  has no concept of manager approval and therefore this method
            // should always return the sender user ID TODO : is this valid for other WFM providers?
            return SenderUserId;
        }

        public void FillTargetIds(IHandledRequest cachedRequest)
        {
            if (cachedRequest != null)
            {
                var cachedOpenShiftRequest = (OpenShiftsChangeRequest)cachedRequest;
                WfmOpenShiftId = cachedOpenShiftRequest.WfmOpenShiftId;
                WfmSenderId = cachedOpenShiftRequest.WfmSenderId;
                WfmSenderLoginName = cachedOpenShiftRequest.WfmSenderLoginName;
                WfmManagerId = cachedOpenShiftRequest.WfmManagerId;
                WfmManagerLoginName = cachedOpenShiftRequest.WfmManagerLoginName;
            }
        }

        public WfmOpenShiftRequestModel AsWfmOpenShiftRequest()
        {
            return new WfmOpenShiftRequestModel
            {
                OpenShiftId = WfmOpenShiftId,
                SenderId = WfmSenderId,
                SenderLoginName = WfmSenderLoginName,
                SenderMessage = SenderMessage,
                SenderDateTime = SenderDateTime,
                ManagerId = WfmManagerId,
                ManagerLoginName = WfmManagerLoginName,
                ManagerMessage = ManagerActionMessage,
                ManagerDateTime = ManagerActionDateTime
            };
        }
    }
}