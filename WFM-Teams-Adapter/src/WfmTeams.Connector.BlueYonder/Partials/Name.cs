// ---------------------------------------------------------------------------
// <copyright file="Name.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Models
{
    using Newtonsoft.Json;

    public partial class NameResource
    {
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                {
                    return $"{FirstName} {LastName}";
                }

                return string.Empty;
            }
        }
    }
}
