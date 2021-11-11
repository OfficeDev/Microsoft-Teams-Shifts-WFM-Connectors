// ---------------------------------------------------------------------------
// <copyright file="ChangeSuccessResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.ChangeRequests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.MicrosoftGraph.Models;

    public class ChangeSuccessResult : JsonResult
    {
        public ChangeSuccessResult(ChangeResponse changeResponse)
            : base(changeResponse)
        {
            foreach (var changeItemResponse in changeResponse.Responses)
            {
                if (changeItemResponse.Status == 0)
                {
                    changeItemResponse.Status = (int)HttpStatusCode.OK;
                    if (string.IsNullOrEmpty(changeItemResponse.Body.Etag))
                    {
                        changeItemResponse.Body.Etag = GenerateEtag(changeItemResponse.Id);
                    }
                }
            }
        }

        public ChangeSuccessResult(ChangeResponse changeResponse, ChangeItemRequest changeItemRequest, string eTag = null)
            : base(changeResponse)
        {
            var changeItemResponse = changeResponse.Responses.First(cir => cir.Id == changeItemRequest.Id);
            changeItemResponse.Status = (int)HttpStatusCode.OK;
            changeItemResponse.Body.Etag = eTag ?? GenerateEtag(changeItemResponse.Id);
        }

        public ChangeSuccessResult(ChangeResponse changeResponse, ChangeItemRequest changeItemRequest, List<string> data)
            : base(changeResponse)
        {
            var changeItemResponse = changeResponse.Responses.First(cir => cir.Id == changeItemRequest.Id);
            changeItemResponse.Status = (int)HttpStatusCode.OK;
            changeItemResponse.Body = new ChangeItemDataResponseBody
            {
                Data = data
            };
        }

        private string GenerateEtag(string requestId)
        {
            return $"{requestId}|{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}".ToBase64String();
        }
    }
}
