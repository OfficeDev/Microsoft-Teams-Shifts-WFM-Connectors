// <copyright file="ArgumentHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using static Microsoft.Teams.Shifts.Integration.API.Common.ResponseHelper;

    /// <summary>
    /// A helper class for any argument checks that are needed.
    /// </summary>
    public static class ArgumentHelper
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the object is <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="potentialNullObject">The object.</param>
        /// <param name="name">The name of the object, for logging.</param>
        public static void ThrowIfNull<T>(this T potentialNullObject, string name)
        {
            if (potentialNullObject == null)
            {
                throw new ArgumentNullException(name, $"{name} is null.");
            }
        }

        public static bool ErrorIfNull<T>(
            this T potentialNullObject,
            string id,
            string error,
            out ShiftsIntegResponse response)
        {
            response = null;

            if (potentialNullObject == null)
            {
                response = CreateBadResponse(id, error: error);
                return true;
            }

            return false;
        }
    }
}
