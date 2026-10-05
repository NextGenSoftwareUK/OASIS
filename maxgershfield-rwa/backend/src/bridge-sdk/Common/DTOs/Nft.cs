namespace Common.DTOs;

public sealed record Nft
{
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required string Royality { get; init; }
    public required string Url { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public IReadOnlyDictionary<string, string>? AdditionalMetadata { get; init; }
}
