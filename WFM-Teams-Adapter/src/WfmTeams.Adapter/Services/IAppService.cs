// ---------------------------------------------------------------------------
// <copyright file="IAppService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.IO;
    using System.Threading.Tasks;

    public interface IAppService
    {
        Task<Stream> OpenAppStreamAsync();
    }
}
