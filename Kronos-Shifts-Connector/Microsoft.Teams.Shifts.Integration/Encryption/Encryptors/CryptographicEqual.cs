// <copyright file="CryptographicEqual.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Encryption.Encryptors
{
    using System.Runtime.CompilerServices;

    public static class CryptographicEqualExtension
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool CryptographicEqual(this byte[] first, byte[] second)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (null == first || null == second || first.Length != second.Length)
            {
                return false;
            }

            int result = 0;

            for (int i = 0; i < first.Length; i++)
            {
                result |= unchecked(first[i] - second[i]);
            }

            return 0 == result;
        }
    }
}