// ---------------------------------------------------------------------------
// <copyright file="TracingOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    public class TracingOptions
    {
        public bool TraceEnabled { get; set; }
        public string TraceFolder { get; set; } = "Traces";
        public string TraceIgnore { get; set; } = "vault.azure.net";
    }
}