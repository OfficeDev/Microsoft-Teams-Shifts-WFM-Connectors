// ---------------------------------------------------------------------------
// <copyright file="EmbeddedStringLocalizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Localization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Resources;
    using Microsoft.Extensions.Localization;

    public class EmbeddedStringLocalizer<T> : IStringLocalizer<T>
    {
        public const string DefaultISOLanguageName = "en";

        private static readonly Lazy<IDictionary<(string, string), string>> _cache = new Lazy<IDictionary<(string, string), string>>(LoadCache);

        public LocalizedString this[string name]
        {
            get
            {
                var languageName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                if (_cache.Value.TryGetValue((languageName, name), out var localMessage))
                {
                    return new LocalizedString(name, localMessage);
                }

                if (_cache.Value.TryGetValue((DefaultISOLanguageName, name), out var defaultMessage))
                {
                    return new LocalizedString(name, defaultMessage);
                }

                return new LocalizedString(name, name);
            }
        }

        public LocalizedString this[string name, params object[] arguments] => throw new NotImplementedException();

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static IDictionary<(string, string), string> LoadCache()
        {
            var cache = new Dictionary<(string, string), string>();

            // assumes all resx files [a] in the current project [b] named {prefix}.TypeName.TwoCharacterLanguageName.resource
            var assembly = typeof(T).Assembly;
            var resources = assembly
                .GetManifestResourceNames()
                .Where(n => n.Contains(typeof(T).Name) && n.Contains(".resource"))
                .ToList();

            foreach (var file in resources)
            {
                var segments = file.Split('.');
                var language = segments[segments.Length - 2];

                using (var stream = assembly.GetManifestResourceStream(file))
                using (var reader = new ResourceReader(stream))
                {
                    foreach (DictionaryEntry item in reader)
                    {
                        cache.Add((language, (string)item.Key), (string)item.Value);
                    }
                }
            }

            return cache;
        }
    }
}
