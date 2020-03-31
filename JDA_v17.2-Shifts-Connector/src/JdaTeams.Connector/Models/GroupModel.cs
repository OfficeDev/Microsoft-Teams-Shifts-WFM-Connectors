using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.Models
{
    public class GroupModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }
}
