using JdaTeams.Connector.MicrosoftGraph.Models;
using JdaTeams.Connector.Models;

namespace JdaTeams.Connector.MicrosoftGraph.Mappings
{
    public interface IShiftMap
    {
        ShiftItem MapShift(ShiftModel shift);
    }
}
