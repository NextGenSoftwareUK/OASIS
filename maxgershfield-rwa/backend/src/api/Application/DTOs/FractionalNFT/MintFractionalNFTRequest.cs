namespace Application.DTOs.FractionalNFT;

/// <summary>
/// Request to mint a fractional NFT
/// </summary>
public sealed class MintFractionalNFTRequest
{
    /// <summary>
    /// ID of the RWA asset being fractionalized
    /// </summary>
    public Guid AssetId { get; init; }

    /// <summary>
    /// Fraction amount (0.01 to 1.0, e.g., 0.1 for 10%)
    /// </summary>
    public decimal FractionAmount { get; init; }

    /// <summary>
    /// Buyer's wallet address (Solana)
    /// </summary>
    public string BuyerWallet { get; init; } = string.Empty;

    /// <summary>
    /// Buyer's OASIS Avatar ID
    /// </summary>
    public string BuyerAvatarId { get; init; } = string.Empty;
}



