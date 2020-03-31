using JdaTeams.Connector.MicrosoftGraph.Exceptions;
using JdaTeams.Connector.MicrosoftGraph.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph.Extensions
{
    public static class ObjectExtensions
    {
        public static void ThrowIfError(this object response)
        {
            if(response is GraphErrorContainer)
            {
                throw new MicrosoftGraphException(((GraphErrorContainer)response).Error);
            }
        }

    }
}
