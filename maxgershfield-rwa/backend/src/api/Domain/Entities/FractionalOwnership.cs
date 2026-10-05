namespace Domain.Entities;

/// <summary>
/// Tracks fractional NFT ownership for RWA assets
/// </summary>
public sealed class FractionalOwnership : BaseEntity
{
    /// <summary>
    /// Reference to the RWA token/asset
    /// </summary>
    public Guid RwaTokenId { get; set; }
    public RwaToken RwaToken { get; set; } = default!;

    /// <summary>
    /// Buyer's wallet linked account
    /// </summary>
    public Guid BuyerWalletLinkedAccountId { get; set; }
    public WalletLinkedAccount BuyerWalletLinkedAccount { get; set; } = default!;

    /// <summary>
    /// Fraction amount (0.01 to 1.0)
    /// </summary>
    public decimal FractionAmount { get; set; }

    /// <summary>
    /// Number of tokens representing this fraction
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// NFT mint address on Solana
    /// </summary>
    public string MintAddress { get; set; } = string.Empty;

    /// <summary>
    /// Transaction hash of the mint transaction
    /// </summary>
    public string MintTransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Transaction hash of the transfer to buyer (if transferred)
    /// </summary>
    public string? TransferTransactionHash { get; set; }

    /// <summary>
    /// Whether the NFT was successfully transferred to buyer
    /// </summary>
    public bool TransferSuccessful { get; set; }

    /// <summary>
    /// IPFS URL of the NFT metadata JSON
    /// </summary>
    public string MetadataUrl { get; set; } = string.Empty;
}



