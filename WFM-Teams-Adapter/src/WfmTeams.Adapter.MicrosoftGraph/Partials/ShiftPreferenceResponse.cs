// ---------------------------------------------------------------------------
// <copyright file="ShiftPreferenceResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using WfmTeams.Adapter.Models;

    public partial class ShiftPreferenceResponse : IHandledRequest
    {
        public string TargetLoginName { get; set; }
        public string TargetUserId { get; set; }

        public string EvaluateTargetLoginName()
        {
            return TargetLoginName;
        }

        public string EvaluateUserId()
        {
            return TargetUserId;
        }

        public void FillTargetIds(IHandledRequest cachedRequest)
        {
            // no implementation required because this method is never called as we do not cache
            // this type of request
        }
    }
}
