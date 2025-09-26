using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EOSNewYork.EOSCore.Serialization
{
    public class TimePointSec : BaseCustomType
    {
        public DateTime value { get; set; }

        public TimePointSec() { }

        public TimePointSec(DateTime value)
        {
            this.value = value;
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(JTokenType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            value = serializer.Deserialize<DateTime>(reader);
            var entity = (TimePointSec)Activator.CreateInstance(objectType, value);
            return entity;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //serialize as actual JSON and not string data
            var token = JToken.Parse(this.value.ToString("s"));
            writer.WriteToken(token.CreateReader());
        }

        public override void WriteToStream(Stream stream)
        {
            var bytes = BitConverter.GetBytes((uint)(value.Ticks / 10000000 - 62135596800)); // 01/01/1970
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}