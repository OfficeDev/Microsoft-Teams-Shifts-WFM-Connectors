// ---------------------------------------------------------------------------
// <copyright file="WfmException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Exceptions
{
    using System;
    using WfmTeams.Adapter.Models;

    /// <summary>
    /// Defines the only exception that is allowed to be thrown by a WFM Connector.
    /// </summary>
    public class WfmException : Exception
    {
        public WfmException()
        {
            Error = new WfmError();
        }

        public WfmException(string message) : base(message)
        {
            Error = new WfmError
            {
                Message = message
            };
        }

        public WfmException(string message, Exception innerException) : base(message, innerException)
        {
            Error = new WfmError
            {
                Code = innerException.GetType().Name,
                Message = message
            };
        }

        public WfmException(WfmError wfmError) : base(wfmError.Message)
        {
            Error = wfmError;
        }

        public WfmException(WfmError wfmError, Exception innerException) : base(wfmError.Message, innerException)
        {
            Error = wfmError;
        }

        public WfmError Error { get; set; }
    }
}
