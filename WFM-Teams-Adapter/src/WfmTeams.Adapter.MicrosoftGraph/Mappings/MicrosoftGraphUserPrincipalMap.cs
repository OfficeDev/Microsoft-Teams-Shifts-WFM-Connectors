// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphUserPrincipalMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Mappings
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using WfmTeams.Adapter.Mappings;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Models;

    public class MicrosoftGraphUserPrincipalMap : IUserPrincipalMap
    {
        private readonly string[] _formats;

        private readonly MicrosoftGraphOptions _options;

        public MicrosoftGraphUserPrincipalMap(MicrosoftGraphOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _formats = options.UserPrincipalNameFormatString.Split(',', ';');
        }

        public IEnumerable<string> ConvertUserPrincipalNames(string login)
        {
            if (new EmailAddressAttribute().IsValid(login))
            {
                return new string[] { login };
            }

            return _formats.Select(format => string.Format(format, login));
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
    }
}
