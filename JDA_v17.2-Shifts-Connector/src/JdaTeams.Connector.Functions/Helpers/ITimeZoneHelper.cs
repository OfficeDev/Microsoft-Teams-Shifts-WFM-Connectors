using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Helpers
{
    public interface ITimeZoneHelper
    {
        Task<string> GetAndUpdateTimeZone(string teamId);
        Task<string> GetTimeZone(string teamId, string storeId);
    }
}