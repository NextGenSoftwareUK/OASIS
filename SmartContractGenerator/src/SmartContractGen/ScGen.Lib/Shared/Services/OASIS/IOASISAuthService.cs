namespace ScGen.Lib.Shared.Services.OASIS;

public interface IOASISAuthService
{
    Task<OASISAuthResult?> ValidateTokenAsync(string jwtToken, CancellationToken cancellationToken = default);
    Task<OASISWalletInfo?> GetAvatarWalletAsync(string jwtToken, string avatarId, string providerType = "SolanaOASIS", CancellationToken cancellationToken = default);
}

public class OASISAuthResult
{
    public string AvatarId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Dictionary<string, object>? ProviderWallets { get; set; }
}

public class OASISWalletInfo
{
    public string WalletAddress { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string? PrivateKey { get; set; } // May be null for security
}


