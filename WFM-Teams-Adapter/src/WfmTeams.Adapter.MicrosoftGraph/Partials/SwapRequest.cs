// ---------------------------------------------------------------------------
// <copyright file="SwapRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.MicrosoftGraph.Enums;
    using WfmTeams.Adapter.Models;

    public partial class SwapRequest : IHandledRequest
    {
        [JsonProperty(PropertyName = "eTag")]
        public string Etag { get; set; }

        [JsonIgnore]
        public bool IsApproved => (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Pending, StringComparison.OrdinalIgnoreCase)) || State.Equals(ChangeRequestPhase.Approved, StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsManager => (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Approved, StringComparison.OrdinalIgnoreCase)) || (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public bool IsRecipient => (AssignedTo.Equals(ChangeRequestAssignedTo.Manager, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Pending, StringComparison.OrdinalIgnoreCase)) || (AssignedTo.Equals(ChangeRequestAssignedTo.Recipient, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public bool IsSystem => AssignedTo.Equals(ChangeRequestAssignedTo.System, StringComparison.OrdinalIgnoreCase) && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase);

        [JsonProperty(PropertyName = "targetManagerUserId")]
        public string TargetManagerUserId { get; set; }

        [JsonProperty(PropertyName = "targetManagerLoginName")]
        public string TargetManagerLoginName { get; set; }

        [JsonProperty(PropertyName = "targetRecipientLoginName")]
        public string TargetRecipientLoginName { get; set; }

        [JsonProperty(PropertyName = "targetRecipientShiftId")]
        public string TargetRecipientShiftId { get; set; }

        [JsonProperty(PropertyName = "targetRecipientUserId")]
        public string TargetRecipientUserId { get; set; }

        [JsonProperty(PropertyName = "targetSenderLoginName")]
        public string TargetSenderLoginName { get; set; }

        [JsonProperty(PropertyName = "targetSenderShiftId")]
        public string TargetSenderShiftId { get; set; }

        [JsonProperty(PropertyName = "targetSenderUserId")]
        public string TargetSenderUserId { get; set; }

        [JsonProperty(PropertyName = "targetSwapRequestId")]
        public string TargetSwapRequestId { get; set; }

        public ChangeRequestState EvaluateState(string method)
        {
            if (method.Equals("delete", StringComparison.OrdinalIgnoreCase)
                || (IsSystem && State.Equals(ChangeRequestPhase.Declined, StringComparison.OrdinalIgnoreCase)))
            {
                return ChangeRequestState.RequestCancelled;
            }
            else if (IsRecipient)
            {
                return IsApproved
                    ? ChangeRequestState.RecipientApproved
                    : ChangeRequestState.RecipientDeclined;
            }
            else if (IsManager)
            {
                return IsApproved
                    ? ChangeRequestState.ManagerApproved
                    : ChangeRequestState.ManagerDeclined;
            }

            return ChangeRequestState.RecipientPending;
        }

        public string EvaluateTargetLoginName()
        {
            if (IsRecipient)
            {
                return TargetRecipientLoginName;
            }
            else if (IsManager)
            {
                return TargetManagerLoginName;
            }

            return TargetSenderLoginName;
        }

        public string EvaluateUserId()
        {
            if (IsRecipient)
            {
                return RecipientUserId;
            }
            else if (IsManager)
            {
                return ManagerUserId;
            }

            return SenderUserId;
        }

        public void FillTargetIds(IHandledRequest cachedRequest)
        {
            if (cachedRequest != null)
            {
                var cachedSwapRequest = (SwapRequest)cachedRequest;
                TargetRecipientShiftId = cachedSwapRequest.TargetRecipientShiftId;
                TargetRecipientUserId = cachedSwapRequest.TargetRecipientUserId;
                TargetRecipientLoginName = cachedSwapRequest.TargetRecipientLoginName;
                TargetSenderShiftId = cachedSwapRequest.TargetSenderShiftId;
                TargetSenderUserId = cachedSwapRequest.TargetSenderUserId;
                TargetSenderLoginName = cachedSwapRequest.TargetSenderLoginName;
                TargetManagerUserId = cachedSwapRequest.TargetManagerUserId;
                TargetManagerLoginName = cachedSwapRequest.TargetManagerLoginName;
                TargetSwapRequestId = cachedSwapRequest.TargetSwapRequestId;
            }
        }

        public WfmShiftSwapModel AsWfmSwapRequest()
        {
            return new WfmShiftSwapModel
            {
                SwapRequestId = TargetSwapRequestId,
                SenderId = TargetSenderUserId,
                SenderLoginName = TargetSenderLoginName,
                SenderShiftId = TargetSenderShiftId,
                SenderMessage = SenderMessage,
                SenderDateTime = SenderDateTime,
                RecipientId = TargetRecipientUserId,
                RecipientLoginName = TargetRecipientLoginName,
                RecipientShiftId = TargetRecipientShiftId,
                RecipientMessage = RecipientActionMessage,
                RecipientDateTime = RecipientActionDateTime,
                ManagerId = TargetManagerUserId,
                ManagerLoginName = TargetManagerLoginName,
                ManagerMessage = ManagerActionMessage,
                ManagerDateTime = ManagerActionDateTime,
            };
        }
    }
}
