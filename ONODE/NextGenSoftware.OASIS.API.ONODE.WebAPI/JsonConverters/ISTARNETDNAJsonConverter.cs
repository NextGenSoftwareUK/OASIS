#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.JsonConverters;

/// <summary>
/// Allows System.Text.Json to deserialize ISTARNETDNA properties (e.g. STARNETDNA on STARNETHolon/InventoryItem) to the concrete STARNETDNA type.
/// Fixes "Deserialization of interface types is not supported" when binding request bodies that include a STARNETDNA property.
/// </summary>
public sealed class ISTARNETDNAJsonConverter : JsonConverter<ISTARNETDNA>
{
    public override ISTARNETDNA? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<STARNETDNA>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, ISTARNETDNA? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
