using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Convertors
{
    public class PolymorphicConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            var type = jsonDocument.RootElement.GetProperty("type").GetString();

            Type targetType = type switch
            {
                "STARNETDependency" => typeof(STARNETDependency),
                _ => throw new JsonException("Unknown type")
            };

            return (T)JsonSerializer.Deserialize(jsonDocument.RootElement.GetRawText(), targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
