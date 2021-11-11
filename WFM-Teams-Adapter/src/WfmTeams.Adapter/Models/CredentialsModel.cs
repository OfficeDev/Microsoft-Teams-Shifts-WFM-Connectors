// ---------------------------------------------------------------------------
// <copyright file="CredentialsModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    /// <summary>
    /// The model defining the credentials used to authenticate to the WFM provider.
    /// </summary>
    public class CredentialsModel
    {
        public string BaseAddress { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }

        public static CredentialsModel FromLoginName(string loginName)
        {
            return new CredentialsModel
            {
                Username = loginName
            };
        }
    }
}
