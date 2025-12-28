namespace Application.DTOs.FractionalNFT;

/// <summary>
/// NFT metadata structure for fractional ownership
/// </summary>
public sealed class NFTMetadata
{
    /// <summary>
    /// NFT name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// NFT description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Image URL (IPFS)
    /// </summary>
    public string Image { get; init; } = string.Empty;

    /// <summary>
    /// External URL to asset page
    /// </summary>
    public string ExternalUrl { get; init; } = string.Empty;

    /// <summary>
    /// Attributes/traits for the NFT
    /// </summary>
    public List<NFTAttribute> Attributes { get; init; } = [];

    /// <summary>
    /// Properties containing full UAT metadata
    /// </summary>
    public NFTProperties Properties { get; init; } = new();
}

/// <summary>
/// NFT attribute/trait
/// </summary>
public sealed class NFTAttribute
{
    public string TraitType { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// NFT properties containing extended metadata
/// </summary>
public sealed class NFTProperties
{
    /// <summary>
    /// Full UAT metadata JSON (can be serialized)
    /// </summary>
    public Dictionary<string, object> UatMetadata { get; init; } = new();

    /// <summary>
    /// Asset ID
    /// </summary>
    public string AssetId { get; init; } = string.Empty;

    /// <summary>
    /// Mint date (ISO 8601)
    /// </summary>
    public string MintDate { get; init; } = string.Empty;
}



