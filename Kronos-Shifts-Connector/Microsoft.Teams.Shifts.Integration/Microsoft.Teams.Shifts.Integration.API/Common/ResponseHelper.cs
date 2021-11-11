// <copyright file="ResponseHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;

    /// <summary>
    /// A helper class dealing with responses.
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// Creates a response for Shifts.
        /// </summary>
        /// <param name="id">The id for the response.</param>
        /// <param name="statusCode">The status code of the response.</param>
        /// <param name="error">The error message for the response.</param>
        /// <param name="eTag">The eTag.</param>
        /// <returns>A <see cref="ShiftsIntegResponse"/>.</returns>
        public static ShiftsIntegResponse CreateResponse(string id, int statusCode, string error = null, string eTag = null)
        {
            return new ShiftsIntegResponse
            {
                Id = id,
                Status = statusCode,
                Body = new Body
                {
                    Error = new ResponseError { Message = error },
                    ETag = eTag ?? Guid.NewGuid().ToString(),
                },
            };
        }

        /// <summary>
        /// Creates a successful response for Shifts.
        /// </summary>
        /// <param name="id">The id for the response.</param>
        /// <returns>A <see cref="ShiftsIntegResponse"/>.</returns>
        public static ShiftsIntegResponse CreateSuccessfulResponse(string id)
        {
            return CreateResponse(id, 200);
        }

        /// <summary>
        /// Creates a bad response for Shifts.
        /// </summary>
        /// <param name="id">The id for the response.</param>
        /// <param name="statusCode">The status code of the response, defaults to 400.</param>
        /// <param name="error">The error message for the response.</param>
        /// <returns>A <see cref="ShiftsIntegResponse"/>.</returns>
        public static ShiftsIntegResponse CreateBadResponse(string id, int statusCode = 400, string error = null)
        {
            return CreateResponse(id, statusCode, error);
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

        /// <summary>
        /// Generate response to prevent actions.
        /// </summary>
        /// <param name="jsonModel">The request payload.</param>
        /// <param name="errorMessage">Error message to send while preventing action.</param>
        /// <returns>List of ShiftsIntegResponse.</returns>
        public static List<ShiftsIntegResponse> CreateMultipleBadResponses(RequestModel jsonModel, string errorMessage)
        {
            List<ShiftsIntegResponse> shiftsIntegResponses = new List<ShiftsIntegResponse>();
            var integrationResponse = new ShiftsIntegResponse();
            foreach (var item in jsonModel.Requests)
            {
                integrationResponse = CreateBadResponse(item.Id, error: errorMessage);
                shiftsIntegResponses.Add(integrationResponse);
            }

            return shiftsIntegResponses;
        }
    }
}