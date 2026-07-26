using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.JsonConverters;

/// <summary>
/// Allows System.Text.Json to deserialize IHolon properties (e.g. Children on Quest/SemanticHolon) to the concrete Holon type.
/// Write path uses a shared cycle-detection set so Quest/Children/STARNETDNA graphs do not cause infinite recursion.
/// </summary>
public sealed class IHolonJsonConverter : JsonConverter<IHolon>
{
    public override IHolon? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<Holon>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, IHolon? value, JsonSerializerOptions options)
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

/// <summary>
/// Tracks object identity during a single JSON serialization root so custom converters can break cycles
/// (e.g. Quest -> Children/Quests/STARNETDNA -> back to same Quest). Shared by IHolon and ISTARNETDNA converters.
/// </summary>
internal static class JsonCycleTracker
{
    private static readonly AsyncLocal<HashSet<object>?> CurrentSet = new();

    public static HashSet<object> GetCurrentSet()
    {
        var set = CurrentSet.Value;
        if (set == null)
        {
            set = new HashSet<object>(ReferenceEqualityComparer.Instance);
            CurrentSet.Value = set;
        }
        return set;
    }

    /// <summary>
    /// Clears the async-local set when empty so the next serialization root gets a fresh set.
    /// </summary>
    public static void ClearIfEmpty(HashSet<object> set)
    {
        if (set.Count == 0)
            CurrentSet.Value = null;
    }
}
