using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public sealed record class OASISEntity
{
    [JsonPropertyName("numeric_id")]
    public required ulong NumericId { get; init; }

    [JsonPropertyName("guid_id")]
    public required string GuidId { get; init; }

    [JsonPropertyName("info_json")]
    public required string InfoJson { get; init; }

    [JsonPropertyName("entity_type")]
    public required OASISEntityType EntityType { get; init; }
}
