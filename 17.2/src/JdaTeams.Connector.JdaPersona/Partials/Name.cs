using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.JdaPersona.Models
{
    public partial class Name
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
