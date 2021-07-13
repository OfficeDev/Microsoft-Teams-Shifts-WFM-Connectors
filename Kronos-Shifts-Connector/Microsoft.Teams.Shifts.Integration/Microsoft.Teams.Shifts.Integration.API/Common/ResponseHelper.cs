// <copyright file="ResponseHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System.Collections.Generic;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;

    /// <summary>
    /// A helper class dealing with responses.
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// Creates a Response for Shifts.
        /// </summary>
        /// <param name="id">The id for the response.</param>
        /// <param name="statusCode">The status code of the response.</param>
        /// <param name="error">The error message for the response.</param>
        /// <returns>A <see cref="ShiftsIntegResponse"/>.</returns>
        public static ShiftsIntegResponse CreateResponse(string id, int statusCode, string error = null)
        {
            return new ShiftsIntegResponse
            {
                Id = id,
                Status = statusCode,
                Body = new Body
                {
                    Error = new ResponseError { Message = error },
                    ETag = null,
                },
            };
        }

        /// <summary>
        /// Creates a Response for a swap shift eligibility request.
        /// </summary>
        /// <param name="id">The id for the response.</param>
        /// <param name="statusCode">The status code of the response.</param>
        /// <param name="shifts">The error message for the response.</param>
        /// <returns>A <see cref="ShiftsIntegResponse"/>.</returns>
        public static ShiftsIntegResponse CreateResponse(string id, int statusCode, IEnumerable<string> shifts)
        {
            return new ShiftsIntegResponse
            {
                Id = id,
                Status = statusCode,
                Body = new Body
                {
                    Data = shifts,
                },
            };
        }
    }
}
