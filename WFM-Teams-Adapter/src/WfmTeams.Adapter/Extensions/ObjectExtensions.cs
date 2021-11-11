// ---------------------------------------------------------------------------
// <copyright file="ObjectExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Extensions
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines generic object extension methods.
    /// </summary>
    public static class ObjectExtensions
    {
        public static T Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static bool IsValid(this object model)
        {
            if (model == null)
            {
                return false;
            }

            var validationContext = new ValidationContext(model);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(model, validationContext, validationResult, validateAllProperties: true);
        }
    }
}
