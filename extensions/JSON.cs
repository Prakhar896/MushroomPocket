using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Extensions {
    public class JSON {
        public enum SerializationOptions {
            Standard,
            DateTimeConverter,
            NullValueHandling
        }

        public static string Serialize(object obj) {
            return JsonConvert.SerializeObject(obj);
        }

        public static void Serialize(object obj, SerializationOptions[] options, StreamWriter streamWriter) {
            if (options.Length == 0 || options.Contains(SerializationOptions.Standard)) {
                streamWriter.Write(JsonConvert.SerializeObject(obj));
                return;
            } else {
                if (streamWriter == null) {
                    throw new ArgumentNullException("streamWriter");
                }

                JsonSerializer serializer = new JsonSerializer();
                if (options.Contains(SerializationOptions.DateTimeConverter)) {
                    // serializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter());
                    serializer.Converters.Add(new JavaScriptDateTimeConverter());
                }

                if (options.Contains(SerializationOptions.NullValueHandling)) {
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                }

                using (JsonWriter writer = new JsonTextWriter(streamWriter)) {
                    serializer.Serialize(writer, obj);
                }
                return;
            }
        }

        #nullable enable
        public static T? Deserialize<T>(string json) {
            return JsonConvert.DeserializeObject<T>(json);
        }
        #nullable disable
    }
}