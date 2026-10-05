using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// QuickBooks OAuth2 tokens
/// </summary>
public class QuickBooksTokens
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string RealmId { get; set; } // QuickBooks company ID
    
    public QuickBooksTokens()
    {
        ExpiresAt = DateTime.UtcNow;
    }
    
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // Refresh 5 minutes before expiry
    }
}




