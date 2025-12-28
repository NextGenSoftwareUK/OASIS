namespace Application.Contracts;

using Application.DTOs.Account.OASIS;
using Application.DTOs.Account.Requests;

/// <summary>
/// Service for OASIS Avatar API authentication and user management
/// </summary>
public interface IOASISAuthService
{
    /// <summary>
    /// Register a new user via OASIS Avatar API
    /// </summary>
    Task<OASISResult<AvatarAuthResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticate user (login) via OASIS Avatar API
    /// </summary>
    Task<OASISResult<AvatarAuthResponse>> LoginAsync(LoginRequest request);

    /// <summary>
    /// Get Avatar details by Avatar ID
    /// </summary>
    Task<OASISResult<Avatar>> GetAvatarAsync(string avatarId);

    /// <summary>
    /// Sync Avatar profile data with local user
    /// </summary>
    Task<OASISResult<bool>> SyncAvatarAsync(string avatarId);

    /// <summary>
    /// Link wallet address to Avatar
    /// </summary>
    Task<OASISResult<string>> LinkWalletAsync(string avatarId, string walletAddress, string network);
}



