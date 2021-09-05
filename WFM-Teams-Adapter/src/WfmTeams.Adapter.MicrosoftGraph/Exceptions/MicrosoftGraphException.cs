// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

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
