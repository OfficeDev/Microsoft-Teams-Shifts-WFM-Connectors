using JdaTeams.Connector.Models;
using System.Collections.Generic;

namespace JdaTeams.Connector.Services
{
    public interface IScheduleDeltaService
    {
        DeltaModel ComputeDelta(IEnumerable<ShiftModel> from, IEnumerable<ShiftModel> to);
    }
}
