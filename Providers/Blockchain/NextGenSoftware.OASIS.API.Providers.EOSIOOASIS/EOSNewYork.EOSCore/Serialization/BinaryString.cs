using System;
using System.IO;
using Cryptography.ECDSA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EOSNewYork.EOSCore.Serialization
{
    public class BinaryString : BaseCustomType
    {
        public string value { get; set; }

        public BinaryString() { }

        public BinaryString(string value)
        {
            this.value = value;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            value = serializer.Deserialize<string>(reader);
            var @object = (BinaryString)Activator.CreateInstance(objectType, value);
            return @object;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //serialize as actual JSON and not string data
            var token = JToken.Parse(value.ToString());
            writer.WriteToken(token.CreateReader());

        }

        public override void WriteToStream(Stream stream)
        {
            var bytes = Hex.HexToBytes(value);
            var length = new UnsignedInt((uint)bytes.Length);
            length.WriteToStream(stream);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
