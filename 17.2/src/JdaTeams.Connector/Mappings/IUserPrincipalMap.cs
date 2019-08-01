using JdaTeams.Connector.Models;
using System.Collections.Generic;

namespace JdaTeams.Connector.Mappings
{
    public interface IUserPrincipalMap
    {
        EmployeeModel MapEmployee(string login, IDictionary<string, EmployeeModel> employees);
    }
}
