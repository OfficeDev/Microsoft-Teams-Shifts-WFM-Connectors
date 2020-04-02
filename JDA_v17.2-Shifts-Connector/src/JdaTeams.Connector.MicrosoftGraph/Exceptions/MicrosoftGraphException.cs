using System;
using System.Runtime.Serialization;
using JdaTeams.Connector.MicrosoftGraph.Models;

namespace JdaTeams.Connector.MicrosoftGraph.Exceptions
{
    [Serializable]
    public class MicrosoftGraphException : Exception
    {
        public MicrosoftGraphException()
        {
        }

        public MicrosoftGraphException(GraphError error)
        {
            Error = error;
        }

        public MicrosoftGraphException(string message) : base(message)
        {
        }

        public MicrosoftGraphException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MicrosoftGraphException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public GraphError Error { get; private set; }
    }
}