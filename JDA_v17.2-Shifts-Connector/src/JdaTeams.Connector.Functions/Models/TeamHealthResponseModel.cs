using JdaTeams.Connector.Models;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Models
{
    public class TeamHealthResponseModel
    {
        public string TeamId { get; set;  }
        public DateTime WeekStartDate { get; set; }

        public List<EmployeeModel> MissingUsers { get; set; } = new List<EmployeeModel>();
        public List<ShiftModel> MissingShifts { get; set; } = new List<ShiftModel>();
        public List<ShiftModel> CachedShifts { get; set; } = new List<ShiftModel>();
        public DurableOrchestrationStatus TeamOrchestratorStatus { get; set; }
    }
}
