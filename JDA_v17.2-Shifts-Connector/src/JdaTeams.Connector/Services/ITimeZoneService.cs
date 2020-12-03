using System.Threading.Tasks;

namespace JdaTeams.Connector.Services
{
    public interface ITimeZoneService
    {
        Task<string> GetTimeZoneInfoIdAsync(string jdaTimeZoneName);
    }
}