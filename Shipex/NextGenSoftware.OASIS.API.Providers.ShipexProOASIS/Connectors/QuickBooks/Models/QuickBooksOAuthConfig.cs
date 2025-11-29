namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models
{
    /// <summary>
    /// QuickBooks OAuth2 configuration
    /// </summary>
    public class QuickBooksOAuthConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Scope { get; set; } = "com.intuit.quickbooks.accounting";
        public string AuthUrl { get; set; } = "https://appcenter.intuit.com/connect/oauth2";
        public string TokenUrl { get; set; } = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
        public string DiscoveryUrl { get; set; } = "https://appcenter.intuit.com/api/v1/connection/disconnect";
        public bool UseSandbox { get; set; } = true; // Use sandbox by default
    }

    /// <summary>
    /// QuickBooks OAuth token response
    /// </summary>
    public class QuickBooksTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "bearer";
        public int ExpiresIn { get; set; } // Seconds until expiration
        public string RealmId { get; set; } = string.Empty; // Company ID
    }

    /// <summary>
    /// Stored QuickBooks OAuth tokens for a merchant
    /// </summary>
    public class QuickBooksTokens
    {
        public Guid MerchantId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

