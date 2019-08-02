using JdaTeams.Connector.Mappings;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace JdaTeams.Connector.MicrosoftGraph.Mappings
{
    public class MicrosoftGraphUserPrincipalMap : IUserPrincipalMap
    {
        private readonly MicrosoftGraphOptions _options;
        private readonly string[] _formats;

        public MicrosoftGraphUserPrincipalMap(MicrosoftGraphOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _formats = options.UserPrincipalNameFormatString.Split(',', ';');
        }

        public EmployeeModel MapEmployee(string login, IDictionary<string, EmployeeModel> employees)
        {
            var userPrincipalNames = ConvertUserPrincipalNames(login);

            foreach (var userPrincipalName in userPrincipalNames)
            {
                if (employees.TryGetValue(userPrincipalName, out var employee))
                {
                    return employee;
                }
            }

            return null;
        }

        public IEnumerable<string> ConvertUserPrincipalNames(string login)
        {
            if (new EmailAddressAttribute().IsValid(login))
            {
                return new string[] { login };
            }

            return _formats.Select(format => string.Format(format, login));
        }
    }
}
