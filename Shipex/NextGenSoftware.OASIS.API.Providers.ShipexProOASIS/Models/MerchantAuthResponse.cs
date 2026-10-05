using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Response model for merchant authentication
/// </summary>
public class MerchantAuthResponse
{
    public Guid MerchantId { get; set; }
    public string JwtToken { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RateLimitTier { get; set; } = string.Empty;
    public DateTime TokenExpiresAt { get; set; }
}




