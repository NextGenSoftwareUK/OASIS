namespace Application.DTOs.FractionalNFT;

/// <summary>
/// Response from transferring an NFT
/// </summary>
public sealed class TransferNFTResponse
{
    /// <summary>
    /// Transaction hash of the transfer
    /// </summary>
    public string TransactionHash { get; init; } = string.Empty;

    /// <summary>
    /// Whether the transfer was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if transfer failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}



