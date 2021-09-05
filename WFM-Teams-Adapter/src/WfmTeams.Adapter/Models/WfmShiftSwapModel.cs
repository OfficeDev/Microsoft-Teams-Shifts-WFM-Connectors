// ---------------------------------------------------------------------------
// <copyright file="WfmShiftSwapModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    public class WfmShiftSwapModel
    {
        public string BuId { get; set; }
        public DateTime? ManagerDateTime { get; set; }
        public string ManagerId { get; set; }
        public string ManagerLoginName { get; set; }
        public string ManagerMessage { get; set; }
        public DateTime? RecipientDateTime { get; set; }
        public string RecipientId { get; set; }
        public string RecipientLoginName { get; set; }
        public string RecipientMessage { get; set; }
        public string RecipientShiftId { get; set; }
        public DateTime? SenderDateTime { get; set; }
        public string SenderId { get; set; }
        public string SenderLoginName { get; set; }
        public string SenderMessage { get; set; }
        public string SenderShiftId { get; set; }
        public string SwapRequestId { get; set; }
    }
}
