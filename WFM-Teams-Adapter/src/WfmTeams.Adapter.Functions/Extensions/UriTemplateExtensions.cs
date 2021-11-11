// ---------------------------------------------------------------------------
// <copyright file="UriTemplateExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Extensions
{
    using System;
    using System.Collections.Generic;
    using Tavis.UriTemplates;

    public static class UriTemplateExtensions
    {
        public static bool TryMatch(this UriTemplate uriTemplate, Uri uri, out IDictionary<string, object> parameters)
        {
            parameters = uriTemplate.GetParameters(uri);

            return parameters != null;
        }

        public static bool TryMatch(this UriTemplate uriTemplate, string uri, out IDictionary<string, object> parameters)
        {
            if (!Uri.TryCreate(uri, UriKind.Relative, out var parsedUri))
            {
                parameters = null;

                return false;
            }

            return TryMatch(uriTemplate, parsedUri, out parameters);
        }
    }
}
