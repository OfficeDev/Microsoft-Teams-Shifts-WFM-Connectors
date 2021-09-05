// ---------------------------------------------------------------------------
// <copyright file="IHandledRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    /// <summary>
    /// Defines the interface that must be implemented by any workforce integration entity.
    /// </summary>
    public interface IHandledRequest
    {
        string EvaluateTargetLoginName();

        string EvaluateUserId();

        void FillTargetIds(IHandledRequest cachedRequest);
    }
}
