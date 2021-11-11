// ---------------------------------------------------------------------------
// <copyright file="ObjectExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Extensions
{
    using WfmTeams.Adapter.MicrosoftGraph.Exceptions;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public static class ObjectExtensions
    {
        public static void ThrowIfError(this object response)
        {
            if (response is GraphErrorContainer)
            {
                throw new MicrosoftGraphException(((GraphErrorContainer)response).Error);
            }
        }
    }
}
