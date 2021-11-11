// ---------------------------------------------------------------------------
// <copyright file="ChangeItemRequestHeaders.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System;
    using System.Collections.Generic;

    public class ChangeItemRequestHeaders : Dictionary<string, string>
    {
        public const string MSExpiresRequestHeader = "X-MS-Expires";
        public const string MSTransactionIdHeader = "X-MS-Transaction-ID";

        public DateTimeOffset? Expires
        {
            get
            {
                if (TryGetValue(MSExpiresRequestHeader, out var expiry))
                {
                    if (DateTimeOffset.TryParse(expiry, out var expires))
                    {
                        return expires;
                    }
                }

                return null;
            }
        }

        public string TransactionId
        {
            get
            {
                if (TryGetValue(MSTransactionIdHeader, out var transactionId))
                {
                    return transactionId;
                }

                return null;
            }
        }
    }
}
