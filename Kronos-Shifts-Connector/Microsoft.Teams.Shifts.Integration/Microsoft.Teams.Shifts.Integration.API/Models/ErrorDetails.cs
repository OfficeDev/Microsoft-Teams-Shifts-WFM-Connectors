// <copyright file="ErrorDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the error details.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Overriding the ToString() method.
        /// </summary>
        /// <returns>The JSON string.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}