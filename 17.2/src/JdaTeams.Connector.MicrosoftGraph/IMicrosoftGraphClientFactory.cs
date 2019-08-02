using JdaTeams.Connector.MicrosoftGraph.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph
{
    public interface IMicrosoftGraphClientFactory
    {
        IMicrosoftGraphClient CreateClient(MicrosoftGraphOptions options, string teamId);
    }
}
