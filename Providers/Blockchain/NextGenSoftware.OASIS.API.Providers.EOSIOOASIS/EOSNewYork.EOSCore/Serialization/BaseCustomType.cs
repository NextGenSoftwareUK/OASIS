using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EOSNewYork.EOSCore.Serialization
{
    public abstract class BaseCustomType : JsonConverter
    {
        public BaseCustomType() { }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(JTokenType));
        }

        public abstract void WriteToStream(Stream stream);
    }
}