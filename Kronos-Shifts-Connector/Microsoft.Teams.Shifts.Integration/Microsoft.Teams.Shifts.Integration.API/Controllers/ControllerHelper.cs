﻿// <copyright file="ControllerHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A controller helper class.
    /// </summary>
    public static class ControllerHelper
    {
        /// <summary>
        /// Generic method to get the body from a Wfi request.
        /// </summary>
        /// <typeparam name="T">The type of the request.</typeparam>
        /// <param name="jsonModel">The Json payload.</param>
        /// <param name="urlValue">The request type you are looking for.</param>
        /// <param name="approved">Whether you are looking for an approved (true) or denied (false) request.</param>
        /// <returns>The body of the Wfi request.</returns>
        public static T Get<T>(RequestModel jsonModel, string urlValue, bool approved = true)
        {
            var obj = jsonModel?.Requests?.FirstOrDefault(x => x.Url.Contains(urlValue, StringComparison.InvariantCulture));
            T result = default;

            return approved ? JsonConvert.DeserializeObject<T>(obj.Body.ToString()) : result;
        }

        /// <summary>
        /// Generic method to get the body from a Wfi approval request.
        /// </summary>
        /// <typeparam name="T">The type of the request.</typeparam>
        /// <param name="jsonModel">The Json payload.</param>
        /// <param name="urlValue">The request type you are looking for.</param>
        /// <param name="approved">Whether you are looking for an approved (true) or denied (false) request.</param>
        /// <returns>The body of the Wfi request.</returns>
        public static T GetRequest<T>(RequestModel jsonModel, string urlValue, bool approved = true)
        {
            var requests = jsonModel?.Requests?.Where(x => x.Url.Contains(urlValue, StringComparison.InvariantCulture));

            IncomingRequest request;
            if (approved)
            {
                request = requests.FirstOrDefault(c => c.Body?["state"].Value<string>() == ApiConstants.ShiftsApproved
                && c.Body["assignedTo"].Value<string>() == ApiConstants.ShiftsManager);
            }
            else
            {
                request = requests.FirstOrDefault(c => c.Body?["state"].Value<string>() == ApiConstants.ShiftsDeclined
                && c.Body["assignedTo"].Value<string>() == ApiConstants.ShiftsManager);
            }

            return JsonConvert.DeserializeObject<T>(request.Body.ToString());
        }

        /// <summary>
        /// Get System declined requests.
        /// </summary>
        /// <param name="jsonModel">Incoming payload for the request been made in Shifts.</param>
        /// <returns>Alist of auto declined requests.</returns>
        public static List<IncomingRequest> GetAutoDeclinedRequests(RequestModel jsonModel)
        {
            var openShiftRequests = jsonModel?.Requests?.Where(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture));

            // Filter all the system declined requests.
            return openShiftRequests.Where(c => c.Body?["state"].Value<string>() == ApiConstants.Declined
                && c.Body["assignedTo"].Value<string>() == ApiConstants.System).ToList();
        }
    }
}