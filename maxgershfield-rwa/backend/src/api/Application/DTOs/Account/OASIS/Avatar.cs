namespace Application.DTOs.Account.OASIS;

/// <summary>
/// OASIS Avatar data model
/// </summary>
public sealed record Avatar
{
    public string AvatarId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public List<WalletInfo>? Wallets { get; init; }
}

/// <summary>
/// Wallet information linked to Avatar
/// </summary>
public sealed record WalletInfo
{
    public string Address { get; init; } = string.Empty;
    public string Network { get; init; } = string.Empty;
}



