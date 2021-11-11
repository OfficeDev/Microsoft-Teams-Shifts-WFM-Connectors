// ---------------------------------------------------------------------------
// <copyright file="ChangeRequestState.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Enums
{
    public enum ChangeRequestState
    {
        RequestCancelled,
        RecipientPending,
        RecipientApproved,
        RecipientDeclined,
        ManagerPending,
        ManagerApproved,
        ManagerDeclined
    }
}