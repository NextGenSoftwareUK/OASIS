using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EOSNewYork.EOSCore.Serialization
{
    public class BaseName : BaseCustomType
    {
        public string value { get; set; }

        public BaseName() { }

        public BaseName(string value)
        {
            this.value = value;
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(JTokenType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            value = serializer.Deserialize<string>(reader);
            var @object = (BaseName)Activator.CreateInstance(objectType, value);
            return @object;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.Parse(this.value);
            writer.WriteToken(token.CreateReader());
        }

        public override void WriteToStream(Stream stream)
        {
            var bytes = BitConverter.GetBytes(SerializationUtil.StringToULong(value));
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}