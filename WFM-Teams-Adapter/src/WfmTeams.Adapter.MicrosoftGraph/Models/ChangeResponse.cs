// ---------------------------------------------------------------------------
// <copyright file="ChangeResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class ChangeResponse
    {
        public ChangeResponse(ChangeRequest changeRequest)
        {
            // set up the response to have a response item for each request item
            Responses = new List<ChangeItemResponse>(changeRequest.Requests.Length);
            foreach (var request in changeRequest.Requests)
            {
                Responses.Add(new ChangeItemResponse
                {
                    Body = new ChangeItemResponseBody(),
                    Id = request.Id
                });
            }
        }

        [JsonProperty("responses")]
        public IList<ChangeItemResponse> Responses { get; set; }
    }
}
