using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NuBus.Util;

namespace NuBus.Adapter.Extension
{
    public static class XMLSerializerExtensions
    {
        internal static string SerializeToXml<T>(this T value) where T : class
        {
            if (value == null)
            {
                return string.Empty;
            }

            try
            {
                var xmlserializer = new XmlSerializer(
                    value.GetType(), new Type[] { value.GetType() });

                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);

                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "An error occurred Serializing to XML.", ex);
            }
        }

        internal static object SerializeFromXml(string value, Type t)
        {
            Condition.NotNull(value);

            using (TextReader reader = new StringReader(value))
            {
                try
                {
                    return new XmlSerializer(t).Deserialize(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "An error occurred Unserializing from XML.", ex);
                }
            }
        }
    }
}
