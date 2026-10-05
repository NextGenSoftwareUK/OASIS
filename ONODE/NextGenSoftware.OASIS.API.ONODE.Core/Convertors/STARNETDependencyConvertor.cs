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
    // Custom JSON Converter
    public class STARNETDependencyConvertor : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(STARNETDependency);

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("Id", out _))
                {
                    return JsonSerializer.Deserialize<STARNETDependency>(root.GetRawText(), options);
                }
                //else if (root.TryGetProperty("Value", out _))
                //{
                //    return JsonSerializer.Deserialize<TypeB>(root.GetRawText(), options);
                //}
            }
            throw new JsonException("Unknown type");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case STARNETDependency typeA:
                    JsonSerializer.Serialize(writer, typeA, options);
                    break;
                //case TypeB typeB:
                //    JsonSerializer.Serialize(writer, typeB, options);
                //    break;
                default:
                    throw new JsonException("Unknown type");
            }

        }
    }
}
