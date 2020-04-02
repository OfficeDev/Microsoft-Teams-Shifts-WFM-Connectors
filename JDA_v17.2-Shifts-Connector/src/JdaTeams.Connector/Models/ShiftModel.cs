using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.Models
{
    public class ShiftModel
    {
        public ShiftModel(string jdaShiftId)
        {
            JdaShiftId = jdaShiftId ?? throw new ArgumentNullException(nameof(jdaShiftId));
        }

        public string JdaShiftId { get; }
        public string TeamsShiftId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LocalStartDate { get; set; }
        public DateTime LocalEndDate { get; set; }
        public int JdaEmployeeId { get; set; }
        public string JdaEmployeeName { get; set; }
        public string TeamsEmployeeId { get; set; }
        public string JdaJobId { get; set; }
        public string JdaJobName { get; set; }
        public string DepartmentName { get; set; }
        public string TeamsSchedulingGroupId { get; set; }
        public string ThemeCode { get; set; }
        public List<ActivityModel> Activities { get; set; } = new List<ActivityModel>();
        public List<ActivityModel> Jobs { get; set; } = new List<ActivityModel>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var job in Jobs)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }
                sb.Append(job.LocalStartDate.ToString("HH:mm"));
                sb.Append("-");
                sb.Append(job.LocalEndDate.ToString("HH:mm"));
                sb.Append(" ");
                sb.Append(job.Code);
            }

            return sb.ToString();
        }
    }
}