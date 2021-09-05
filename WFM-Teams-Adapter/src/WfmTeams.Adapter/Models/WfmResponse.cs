// ---------------------------------------------------------------------------
// <copyright file="WfmResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    public class WfmResponse
    {
        public WfmError Error { get; set; }

        public bool Success { get; set; }

        public string NewEntityId { get; set; }

        public static WfmResponse SuccessResponse(string newEntityId = null)
        {
            return new WfmResponse
            {
                Success = true,
                NewEntityId = newEntityId
            };
        }

        public static WfmResponse ErrorResponse(string errorCode, string errorMessage)
        {
            return new WfmResponse
            {
                Success = false,
                Error = new WfmError
                {
                    Code = errorCode,
                    Message = errorMessage
                }
            };
        }
    }
}
