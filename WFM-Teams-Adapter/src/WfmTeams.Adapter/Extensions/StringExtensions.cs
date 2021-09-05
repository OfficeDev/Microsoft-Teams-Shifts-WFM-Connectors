// ---------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Extensions
{
    using System;
    using System.Text;

    /// <summary>
    /// Defines string extension methods.
    /// </summary>
    public static class StringExtensions
    {
        public static string FromBase64String(this string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }

        public static string ToBase64String(this string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }
    }
}
