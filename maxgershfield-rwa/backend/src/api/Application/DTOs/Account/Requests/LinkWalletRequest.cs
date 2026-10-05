namespace Application.DTOs.Account.Requests;

/// <summary>
/// Request to link a wallet address to OASIS Avatar
/// </summary>
public sealed record LinkWalletRequest
{
    [Required]
    public string WalletAddress { get; init; } = string.Empty;

    [Required]
    public string Network { get; init; } = string.Empty;
}



