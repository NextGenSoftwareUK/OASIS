namespace NextGenSoftware.OASIS.STARAPI.Client;

public sealed class StarItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string GameSource { get; init; } = "Unknown";
    public string ItemType { get; init; } = "Miscellaneous";
    /// <summary>NFT ID from MetaData when item is linked to an NFTHolon (minted). Empty when not an NFT item.</summary>
    public string NftId { get; init; } = string.Empty;
    /// <summary>Stack size. When adding with stack=true, API increments this if item exists; otherwise new item gets this quantity. Default 1.</summary>
    public int Quantity { get; init; } = 1;
}
