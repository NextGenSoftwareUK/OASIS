namespace Application.DTOs.FractionalNFT;

/// <summary>
/// Response from minting a fractional NFT
/// </summary>
public sealed class MintFractionalNFTResponse
{
    /// <summary>
    /// Mint address of the created NFT
    /// </summary>
    public string MintAddress { get; init; } = string.Empty;

    /// <summary>
    /// Transaction hash of the mint transaction
    /// </summary>
    public string TransactionHash { get; init; } = string.Empty;

    /// <summary>
    /// Number of tokens minted for this fraction
    /// </summary>
    public int TokenCount { get; init; }

    /// <summary>
    /// Fraction amount (e.g., 0.1 for 10%)
    /// </summary>
    public decimal FractionAmount { get; init; }

    /// <summary>
    /// Percentage representation (e.g., 10 for 10%)
    /// </summary>
    public decimal Percentage => FractionAmount * 100;

    /// <summary>
    /// ID of the fractional ownership record in database
    /// </summary>
    public Guid FractionalOwnershipId { get; init; }

    /// <summary>
    /// Whether the NFT was successfully transferred to buyer
    /// </summary>
    public bool TransferSuccessful { get; init; }
}



