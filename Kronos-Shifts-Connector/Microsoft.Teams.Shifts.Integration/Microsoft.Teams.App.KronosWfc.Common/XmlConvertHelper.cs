// <copyright file="XmlConvertHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Common
{
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// XML Convert helper class.
    /// </summary>
    public static class XmlConvertHelper
    {
        /// <summary>
        /// Generic method to serialize data into XML.
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="entity">Generic type parameter.</param>
        /// <returns>Serialized string.</returns>
        public static string XmlSerialize<T>(this T entity)
            where T : class
        {
            // removes version
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;

            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    // removes namespace
                    var xmlns = new XmlSerializerNamespaces();
                    xmlns.Add(string.Empty, string.Empty);

                    xsSubmit.Serialize(writer, entity, xmlns);
                    return sw.ToString(); // Your XML
                }
            }
        }

        /// <summary>
        /// Generic method to deserialize XML back to object.
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="xml">XML string input.</param>
        /// <returns>Generic object.</returns>
        public static T DeserializeObject<T>(this string xml)
            where T : new()
        {
            if (string.IsNullOrEmpty(xml))
            {
                return new T();
            }

            using (var stringReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings() { XmlResolver = null }))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }
    }
}