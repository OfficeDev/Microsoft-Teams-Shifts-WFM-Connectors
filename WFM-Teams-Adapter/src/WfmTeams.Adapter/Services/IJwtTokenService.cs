// ---------------------------------------------------------------------------
// <copyright file="IJwtTokenService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(string sub);

        string ParseToken(string token);
    }
}