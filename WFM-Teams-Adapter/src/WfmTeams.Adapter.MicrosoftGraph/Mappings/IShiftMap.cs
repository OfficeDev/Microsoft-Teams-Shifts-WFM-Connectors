// ---------------------------------------------------------------------------
// <copyright file="IShiftMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Mappings
{
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;

    public interface IShiftMap
    {
        OpenShiftItem MapOpenShift(ShiftModel shift);

        ShiftItem MapShift(ShiftModel shift);
    }
}
