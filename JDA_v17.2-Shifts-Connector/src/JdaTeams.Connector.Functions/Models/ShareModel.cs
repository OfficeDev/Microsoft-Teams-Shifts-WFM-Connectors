using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.Functions.Models
{
    public class ShareModel
    {
        public string TeamId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimeZoneInfoId { get; set; }
    }
}
