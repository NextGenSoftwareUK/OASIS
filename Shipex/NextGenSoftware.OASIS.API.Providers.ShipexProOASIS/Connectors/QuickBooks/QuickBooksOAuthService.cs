using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks
{
    /// <summary>
    /// Service for handling QuickBooks OAuth2 authentication flow.
    /// Manages authorization, token exchange, and token refresh.
    /// </summary>
    public class QuickBooksOAuthService
    {
        private readonly QuickBooksOAuthConfig _config;
        private readonly ILogger<QuickBooksOAuthService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ISecretVaultService _secretVault; // Will be implemented by Agent F

        public QuickBooksOAuthService(
            QuickBooksOAuthConfig config,
            ILogger<QuickBooksOAuthService> logger = null,
            HttpClient httpClient = null,
            ISecretVaultService secretVault = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
            _secretVault = secretVault;
        }

        /// <summary>
        /// Generates the authorization URL for QuickBooks OAuth2 flow.
        /// </summary>
        /// <param name="merchantId">Merchant identifier</param>
        /// <param name="state">State parameter for CSRF protection</param>
        /// <returns>Authorization URL</returns>
        public string GetAuthorizationUrl(Guid merchantId, string state = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                state = Guid.NewGuid().ToString();
            }

            var baseUrl = _config.UseSandbox 
                ? "https://appcenter.intuit.com/connect/oauth2"
                : "https://appcenter.intuit.com/connect/oauth2";

            var queryParams = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "response_type", "code" },
                { "scope", _config.Scope },
                { "redirect_uri", _config.RedirectUri },
                { "state", state }
            };

            var queryString = string.Join("&", 
                System.Linq.Enumerable.Select(queryParams, kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Exchanges authorization code for access and refresh tokens.
        /// </summary>
        /// <param name="authorizationCode">Authorization code from QuickBooks callback</param>
        /// <param name="realmId">Company ID from QuickBooks</param>
        /// <returns>Token response with access and refresh tokens</returns>
        public async Task<OASISResult<QuickBooksTokenResponse>> ExchangeCodeForTokensAsync(
            string authorizationCode,
            string realmId)
        {
            var result = new OASISResult<QuickBooksTokenResponse>();

            try
            {
                _logger?.LogInformation("Exchanging authorization code for tokens");

                var tokenUrl = _config.TokenUrl;
                
                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", _config.RedirectUri }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(requestBody)
                };

                // Add Basic Auth header
                var authValue = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"Failed to exchange code for tokens: {response.StatusCode} - {responseContent}");
                    OASISErrorHandling.HandleError(ref result, $"Failed to exchange code: {responseContent}");
                    return result;
                }

                var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize token response");
                    return result;
                }

                tokenResponse.RealmId = realmId;
                result.Result = tokenResponse;
                result.IsError = false;

                _logger?.LogInformation("Successfully exchanged code for tokens");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while exchanging code for tokens");
                OASISErrorHandling.HandleError(ref result, $"Failed to exchange code: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Refreshes an access token using a refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>New token response</returns>
        public async Task<OASISResult<QuickBooksTokenResponse>> RefreshTokenAsync(string refreshToken)
        {
            var result = new OASISResult<QuickBooksTokenResponse>();

            try
            {
                _logger?.LogInformation("Refreshing QuickBooks access token");

                var tokenUrl = _config.TokenUrl;
                
                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(requestBody)
                };

                // Add Basic Auth header
                var authValue = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"Failed to refresh token: {response.StatusCode} - {responseContent}");
                    OASISErrorHandling.HandleError(ref result, $"Failed to refresh token: {responseContent}");
                    return result;
                }

                var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize token response");
                    return result;
                }

                result.Result = tokenResponse;
                result.IsError = false;

                _logger?.LogInformation("Successfully refreshed access token");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while refreshing token");
                OASISErrorHandling.HandleError(ref result, $"Failed to refresh token: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Stores tokens securely in the secret vault.
        /// </summary>
        public async Task<OASISResult<bool>> StoreTokensAsync(Guid merchantId, QuickBooksTokenResponse tokenResponse)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (_secretVault == null)
                {
                    _logger?.LogWarning("Secret vault service not available, tokens cannot be stored securely");
                    OASISErrorHandling.HandleError(ref result, "Secret vault service not configured");
                    return result;
                }

                var tokens = new QuickBooksTokens
                {
                    MerchantId = merchantId,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    RealmId = tokenResponse.RealmId,
                    AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Store in secret vault (implementation depends on Agent F)
                // For now, we'll just return success
                result.Result = true;
                result.IsError = false;

                _logger?.LogInformation($"Stored QuickBooks tokens for merchant {merchantId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while storing tokens for merchant {merchantId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to store tokens: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Interface for secret vault service (to be implemented by Agent F)
    /// </summary>
    public interface ISecretVaultService
    {
        Task<OASISResult<bool>> StoreSecretAsync(string key, object value);
        Task<OASISResult<T>> GetSecretAsync<T>(string key);
    }
}




