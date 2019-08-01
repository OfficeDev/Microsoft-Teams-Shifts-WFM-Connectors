using System;

namespace JdaTeams.Connector.Models
{
    public class ActivityModel
    {
        public string Code { get; set; }
        public string JdaJobId { get; set; }
        public string DepartmentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LocalStartDate { get; set; }
        public DateTime LocalEndDate { get; set; }
        public string ThemeCode { get; set; }
    }
}