using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.WebAPI.JsonConverters;

/// <summary>
/// Allows System.Text.Json to deserialize ISTARNETDNA properties (e.g. on Quest) to the concrete STARNETDNA type.
/// Write path uses shared cycle detection so nested Dependencies/OAPPs/Quests graphs do not cause infinite recursion.
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
        var set = JsonCycleTracker.GetCurrentSet();
        if (set.Contains(value))
        {
            writer.WriteNullValue();
            return;
        }
        set.Add(value);
        try
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
        finally
        {
            set.Remove(value);
            JsonCycleTracker.ClearIfEmpty(set);
        }
    }
}
