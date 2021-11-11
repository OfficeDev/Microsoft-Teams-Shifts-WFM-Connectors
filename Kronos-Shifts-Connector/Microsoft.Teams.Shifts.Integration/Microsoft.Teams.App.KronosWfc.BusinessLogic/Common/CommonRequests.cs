// <copyright file="CommonRequests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Common
{
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using UpdateStatus = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.UpdateStatus;

    /// <summary>
    /// Methods for building common requests.
    /// </summary>
    public static class CommonRequests
    {
        /// <summary>
        /// Creates an XML request to updatethe status of time off, swap and open shift requests in Kronos.
        /// </summary>
        /// <param name="personNumber">The kronos person number.</param>
        /// <param name="reqId">The request id to update.</param>
        /// <param name="status">The status to update the request to.</param>
        /// <param name="querySpan">The query date span.</param>
        /// <param name="comments">Comments to add to the request.</param>
        /// <returns>String representation of the XML request.</returns>
        public static string CreateUpdateStatusRequest(string personNumber, string reqId, string status, string querySpan, Comments comments)
        {
            var request = new UpdateStatus.Request
            {
                Action = ApiConstants.UpdateStatus,
                RequestMgmt = new UpdateStatus.RequestMgmt
                {
                    Employees = new Employee
                    {
                        PersonIdentity = new PersonIdentity()
                        {
                            PersonNumber = personNumber,
                        },
                    },
                    QueryDateSpan = querySpan,
                    RequestStatusChanges = new UpdateStatus.RequestStatusChanges()
                    {
                        RequestStatusChange = new UpdateStatus.RequestStatusChange[]
                        {
                            new UpdateStatus.RequestStatusChange
                            {
                                RequestId = reqId,
                                ToStatusName = status,
                                Comments = comments,
                            },
                        },
                    },
                },
            };

            return request.XmlSerialize();
        }
    }
}