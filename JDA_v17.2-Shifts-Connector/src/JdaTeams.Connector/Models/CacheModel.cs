using System.Collections.Generic;
using System.Linq;

namespace JdaTeams.Connector.Models
{
    public class CacheModel
    {
        public CacheModel()
        {

        }

        public CacheModel(IEnumerable<ShiftModel> trackedShifts)
        {
            Tracked = trackedShifts.ToList();
        }

        public List<ShiftModel> Tracked { get; set; } = new List<ShiftModel>();
        public List<string> Skipped { get; set; } = new List<string>();
    }
}
