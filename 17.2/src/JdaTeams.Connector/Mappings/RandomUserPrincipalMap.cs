using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JdaTeams.Connector.Mappings
{
    public class RandomUserPrincipalMap : IUserPrincipalMap
    {
        public EmployeeModel MapEmployee(string login, IDictionary<string, EmployeeModel> employees)
        {
            return employees.GetValueOrDefault(login) 
                ?? employees.Values.OrderBy(e => Guid.NewGuid()).FirstOrDefault();
        }
    }
}
