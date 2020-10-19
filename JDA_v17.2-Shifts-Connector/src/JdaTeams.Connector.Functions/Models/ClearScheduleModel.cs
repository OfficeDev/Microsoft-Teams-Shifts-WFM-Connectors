using System;
using System.ComponentModel.DataAnnotations;

namespace JdaTeams.Connector.Functions.Models
{
    public class ClearScheduleModel
    {
        [Required]
        public string TeamId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? PastWeeks { get; set; }

        public int? FutureWeeks { get; set; }

        public bool ClearSchedulingGroups { get; set; } = true;

        public string InstanceId => TeamId + "-ClearSchedule";

        public DateTime? QueryEndDate { get; set; }
        public string TimeZoneInfoId { get; internal set; }
    }
}
