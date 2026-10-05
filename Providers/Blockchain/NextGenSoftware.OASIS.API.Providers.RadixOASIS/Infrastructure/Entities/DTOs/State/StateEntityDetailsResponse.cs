using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.State;

/// <summary>
/// Response from Gateway API state entity details endpoint
/// </summary>
public class StateEntityDetailsResponse
{
    [JsonPropertyName("items")]
    public List<EntityDetailsItem> Items { get; set; } = new();
}

/// <summary>
/// Individual entity details item
/// </summary>
public class EntityDetailsItem
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentState? State { get; set; }

    // Additional fields may be present depending on entity type
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Component state structure (for component entities)
/// </summary>
public class ComponentState
{
    [JsonPropertyName("fields")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentFields? Fields { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Component fields including KeyValueStore entries
/// </summary>
public class ComponentFields
{
    [JsonPropertyName("key_value_store_entries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, KeyValueStoreEntry>? KeyValueStoreEntries { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// KeyValueStore entry with key and value
/// </summary>
public class KeyValueStoreEntry
{
    [JsonPropertyName("key")]
    public object? Key { get; set; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public KeyValueStoreValue? Value { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// KeyValueStore value structure
/// </summary>
public class KeyValueStoreValue
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    [JsonPropertyName("value_bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueBytes { get; set; }

    // For JSON strings stored in KeyValueStore
    [JsonPropertyName("string_value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StringValue { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

