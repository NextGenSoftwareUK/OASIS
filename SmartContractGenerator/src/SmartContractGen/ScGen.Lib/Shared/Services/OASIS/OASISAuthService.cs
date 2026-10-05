using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScGen.Lib.Shared.Services.OASIS;

public class OASISAuthService : IOASISAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OASISAuthService> _logger;
    private readonly OASISOptions _options;

    public OASISAuthService(
        HttpClient httpClient,
        ILogger<OASISAuthService> logger,
        IOptions<OASISOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OASISAuthResult?> ValidateTokenAsync(string jwtToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate token by getting avatar wallets (which requires auth)
            // Extract avatar ID from JWT token payload (basic extraction)
            // Or use a token validation endpoint if available
            
            // For now, we'll validate by trying to get wallets
            // The actual avatar ID should come from the token or be passed separately
            // This is a simplified version - in production, decode JWT to get avatar ID
            
            // Try to get avatar info from token (decode JWT)
            var tokenParts = jwtToken.Split('.');
            if (tokenParts.Length < 2)
            {
                _logger.LogWarning("Invalid JWT token format");
                return null;
            }

            // Decode payload (base64url)
            var payload = tokenParts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            
            var payloadBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            var payloadDoc = JsonDocument.Parse(payloadJson);
            
            var avatarId = payloadDoc.RootElement.GetProperty("avatarId").GetString() 
                        ?? payloadDoc.RootElement.GetProperty("id").GetString() 
                        ?? payloadDoc.RootElement.GetProperty("sub").GetString();
            
            if (string.IsNullOrEmpty(avatarId))
            {
                _logger.LogWarning("Could not extract avatar ID from token");
                return null;
            }

            // Get wallets to validate token and get wallet info
            var walletInfo = await GetAvatarWalletAsync(jwtToken, avatarId, "SolanaOASIS", cancellationToken);
            
            return new OASISAuthResult
            {
                AvatarId = avatarId,
                Username = payloadDoc.RootElement.TryGetProperty("username", out var username) 
                    ? username.GetString() ?? string.Empty 
                    : string.Empty,
                ProviderWallets = null // Will be populated when getting wallet
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OASIS token");
            return null;
        }
    }

    public async Task<OASISWalletInfo?> GetAvatarWalletAsync(
        string jwtToken, 
        string avatarId, 
        string providerType = "SolanaOASIS", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get wallets for avatar
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/wallet/avatar/{avatarId}/wallets");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get avatar wallets: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            var wallets = root.GetProperty("result").EnumerateArray();
            
            foreach (var wallet in wallets)
            {
                var walletProviderType = wallet.GetProperty("providerType").GetString();
                if (walletProviderType == providerType)
                {
                    return new OASISWalletInfo
                    {
                        WalletAddress = wallet.GetProperty("walletAddress").GetString() ?? 
                                       wallet.GetProperty("publicKey").GetString() ?? string.Empty,
                        WalletId = wallet.GetProperty("id").GetString() ?? 
                                  wallet.GetProperty("walletId").GetString() ?? string.Empty,
                        PublicKey = wallet.GetProperty("publicKey").GetString() ?? string.Empty,
                        ProviderType = walletProviderType ?? string.Empty
                        // Private key not included for security
                    };
                }
            }

            _logger.LogWarning("No {ProviderType} wallet found for avatar {AvatarId}", providerType, avatarId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatar wallet");
            return null;
        }
    }

    private Dictionary<string, object>? ExtractProviderWallets(JsonElement result)
    {
        if (!result.TryGetProperty("providerWallets", out var wallets))
            return null;

        var dict = new Dictionary<string, object>();
        foreach (var prop in wallets.EnumerateObject())
        {
            dict[prop.Name] = prop.Value;
        }
        return dict;
    }
}

public class OASISOptions
{
    public string ApiUrl { get; set; } = "http://api.oasisweb4.com";
}

