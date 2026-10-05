namespace SolanaBridge.Nft;

public sealed record MetaplexNft
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("symbol")] public required string Symbol { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("image")] public string? Image { get; init; }

    [JsonPropertyName("attributes")] public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}