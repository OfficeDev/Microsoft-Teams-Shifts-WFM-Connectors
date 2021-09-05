// ---------------------------------------------------------------------------
// <copyright file="HttpRequestExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Extensions
{
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Teams.Shifts.Encryption.Encryptors;
    using Newtonsoft.Json;

    public static class HttpRequestExtensions
    {
        public static void ApplyThreadCulture(this HttpRequest request)
        {
            try
            {
                var cultureName = request.GetTypedHeaders()
                    .AcceptLanguage?
                    .Where(h => h.Value.HasValue)
                    .Select(h => h.Value.Value)
                    .FirstOrDefault();

                if (cultureName == null)
                {
                    return;
                }

                var cultureInfo = CultureInfo.GetCultureInfo(cultureName);

                if (cultureInfo != null)
                {
                    CultureInfo.CurrentCulture = cultureInfo;
                    CultureInfo.CurrentUICulture = cultureInfo;
                }
            }
            catch
            {
                return;
            }
        }

        public static async Task<T> ReadAsObjectAsync<T>(this HttpRequest request, string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                return JsonConvert.DeserializeObject<T>(await request.ReadAsStringAsync());
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var decryptor = new Aes256CbcHmacSha256Encryptor(secretBytes);
            using (var ms = new MemoryStream())
            {
                await request.Body.CopyToAsync(ms);
                var decryptedPayload = decryptor.Decrypt(ms.ToArray());

                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(decryptedPayload));
            }
        }

        public static T ReadHeaderAs<T>(this HttpRequest request, string key)
        {
            if (request.Headers?.ContainsKey(key) == true)
            {
                var value = request.Headers[key];
                if (value.Count > 0)
                {
                    // TODO: enhance this solution to deal with the scenario where value contains more than a single element
                    var converter = TypeDescriptor.GetConverter(typeof(T));
                    return converter.CanConvertFrom(typeof(string)) ? (T)converter.ConvertFrom(value[0]) : default;
                }
            }

            return default;
        }
    }
}
