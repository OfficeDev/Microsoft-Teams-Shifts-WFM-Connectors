// ---------------------------------------------------------------------------
// <copyright file="ChangeErrorResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.ChangeRequests
{
    using System.Linq;
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public class ChangeErrorResult : JsonResult
    {
        public ChangeErrorResult(ChangeResponse changeResponse, string errorCode, string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(changeResponse)
        {
            foreach (var responseItem in changeResponse.Responses)
            {
                responseItem.Status = (int)statusCode;
                responseItem.Body.Error = new ChangeErrorResponse
                {
                    Code = errorCode,
                    Message = errorMessage
                };
            }

            // notwithstanding that this is an error result, the status code must still be 200 OK
            this.StatusCode = (int)HttpStatusCode.OK;
        }

        public ChangeErrorResult(ChangeResponse changeResponse, ChangeItemRequest changeItemRequest, string errorCode, string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : this(changeResponse, changeItemRequest, errorCode, errorMessage, false, statusCode)
        {
        }

        public ChangeErrorResult(ChangeResponse changeResponse, ChangeItemRequest changeItemRequest, string errorCode, string errorMessage, bool dataErrorResponse, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(changeResponse)
        {
            var changeItemResponse = changeResponse.Responses.First(cir => cir.Id == changeItemRequest.Id);
            changeItemResponse.Status = (int)statusCode;
            if (dataErrorResponse)
            {
                changeItemResponse.Body = new ChangeItemDataResponseBody();
            }

            changeItemResponse.Body.Error = new ChangeErrorResponse
            {
                Code = errorCode,
                Message = errorMessage
            };

            // notwithstanding that this is an error result, the status code must still be 200 OK
            this.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
