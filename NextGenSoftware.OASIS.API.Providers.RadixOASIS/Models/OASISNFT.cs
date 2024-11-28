using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public sealed record class OASISNFT
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}
