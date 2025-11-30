using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Service for minting NFTs via the OASIS NFT API
    /// Handles authentication, provider activation, and NFT minting
    /// </summary>
    public class NFTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<NFTService> _logger;
        private readonly string _avatarUsername;
        private readonly string _avatarPassword;
        private readonly Guid _botAvatarId;
        
        private string _jwtToken;
        private DateTime _tokenExpiry;
        private bool _providersActivated;

        public NFTService(
            string baseUrl, 
            string avatarUsername, 
            string avatarPassword,
            Guid botAvatarId,
            ILogger<NFTService> logger = null)
        {
            // For local testing, ignore SSL certificate errors
            var handler = new HttpClientHandler();
            if (baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1"))
            {
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromMinutes(2); // NFT minting can take time
            _baseUrl = baseUrl;
            _avatarUsername = avatarUsername;
            _avatarPassword = avatarPassword;
            _botAvatarId = botAvatarId;
            _logger = logger;
            _providersActivated = false;
        }

        /// <summary>
        /// Authenticate and get JWT token
        /// </summary>
        private async Task<bool> EnsureAuthenticatedAsync()
        {
            try
            {
                // Check if token is still valid (with 5 minute buffer)
                if (!string.IsNullOrEmpty(_jwtToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return true;
                }

                _logger?.LogInformation("[NFTService] Authenticating bot avatar...");

                var authPayload = new
                {
                    username = _avatarUsername,
                    password = _avatarPassword
                };

                var json = JsonSerializer.Serialize(authPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/avatar/authenticate", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger?.LogInformation($"[NFTService] Auth response status: {response.StatusCode}");
                _logger?.LogInformation($"[NFTService] Auth response body: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError($"[NFTService] Authentication failed with status {response.StatusCode}: {responseBody}");
                    return false;
                }

                // Parse response to get JWT token
                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("jwtToken", out var tokenElement))
                    {
                        _jwtToken = tokenElement.GetString();
                        _tokenExpiry = DateTime.UtcNow.AddHours(1); // Tokens typically last 1 hour
                        
                        _logger?.LogInformation("[NFTService] Successfully authenticated");
                        return true;
                    }
                }

                _logger?.LogError($"[NFTService] No JWT token in authentication response. Full response: {responseBody}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[NFTService] Authentication error");
                return false;
            }
        }

        /// <summary>
        /// Register and activate OASIS providers (SolanaOASIS and MongoDBOASIS)
        /// </summary>
        private async Task<bool> EnsureProvidersActivatedAsync()
        {
            try
            {
                if (_providersActivated)
                {
                    return true;
                }

                if (!await EnsureAuthenticatedAsync())
                {
                    return false;
                }

                _logger?.LogInformation("[NFTService] Activating providers...");

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                // Register and activate SolanaOASIS
                var solanaRegister = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/provider/register-provider-type/SolanaOASIS", 
                    null
                );
                
                if (!solanaRegister.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("[NFTService] SolanaOASIS registration failed (may already be registered)");
                }

                var solanaActivate = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/provider/activate-provider/SolanaOASIS",
                    null
                );

                if (!solanaActivate.IsSuccessStatusCode)
                {
                    var error = await solanaActivate.Content.ReadAsStringAsync();
                    _logger?.LogError($"[NFTService] SolanaOASIS activation failed: {error}");
                    return false;
                }

                // Register and activate MongoDBOASIS
                var mongoRegister = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/provider/register-provider-type/MongoDBOASIS",
                    null
                );

                if (!mongoRegister.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("[NFTService] MongoDBOASIS registration failed (may already be registered)");
                }

                var mongoActivate = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/provider/activate-provider/MongoDBOASIS",
                    null
                );

                if (!mongoActivate.IsSuccessStatusCode)
                {
                    var error = await mongoActivate.Content.ReadAsStringAsync();
                    _logger?.LogError($"[NFTService] MongoDBOASIS activation failed: {error}");
                    return false;
                }

                _providersActivated = true;
                _logger?.LogInformation("[NFTService] Providers activated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[NFTService] Provider activation error");
                return false;
            }
        }

        /// <summary>
        /// Mint an achievement NFT badge
        /// </summary>
        public async Task<OASISResult<string>> MintAchievementNFTAsync(
            string title,
            string description,
            string recipientWallet,
            Guid mintedByAvatarId,
            string symbol = "MMASON",
            string imageUrl = null,
            string jsonMetadataUrl = null,
            decimal price = 0,
            bool storeOnChain = false)
        {
            try
            {
                _logger?.LogInformation($"[NFTService] Minting NFT: {title} for wallet {recipientWallet}");

                // Ensure we're authenticated and providers are activated
                if (!await EnsureProvidersActivatedAsync())
                {
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = "Failed to authenticate or activate providers. Check bot avatar credentials in OASIS_DNA.json"
                    };
                }

                // Set authorization header for mint request
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");

                // Use bot avatar ID for minting (not the user's avatar)
                mintedByAvatarId = _botAvatarId;

                // Build the mint payload matching your frontend structure
                var payload = new
                {
                    Title = title,
                    Description = description,
                    Symbol = symbol,
                    OnChainProvider = new { value = 3, name = "SolanaOASIS" },
                    OffChainProvider = new { value = 23, name = "MongoDBOASIS" },
                    NFTOffChainMetaType = new { value = 3, name = "ExternalJsonURL" },
                    NFTStandardType = new { value = 2, name = "SPL" },
                    ImageUrl = imageUrl ?? "https://via.placeholder.com/500?text=Achievement+Badge",
                    JSONMetaDataURL = jsonMetadataUrl ?? "",
                    ThumbnailUrl = imageUrl ?? "https://via.placeholder.com/150?text=Badge",
                    Price = price,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = storeOnChain,
                    MintedByAvatarId = mintedByAvatarId.ToString(),
                    SendToAddressAfterMinting = recipientWallet,
                    WaitTillNFTSent = true,
                    WaitForNFTToSendInSeconds = 90,
                    AttemptToSendEveryXSeconds = 5
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                _logger?.LogInformation($"[NFTService] Payload: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var endpoint = $"{_baseUrl}/api/nft/mint-nft";
                _logger?.LogInformation($"[NFTService] Calling endpoint: {endpoint}");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger?.LogInformation($"[NFTService] Response Status: {response.StatusCode}");
                _logger?.LogInformation($"[NFTService] Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    return new OASISResult<string>
                    {
                        Result = responseBody,
                        Message = "NFT minted successfully! Check the recipient's Solana wallet.",
                        IsError = false,
                        IsSaved = true
                    };
                }
                else
                {
                    _logger?.LogError($"[NFTService] Minting failed with status {response.StatusCode}: {responseBody}");
                    
                    return new OASISResult<string>
                    {
                        Message = $"NFT minting failed: {response.StatusCode} - {responseBody}",
                        IsError = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[NFTService] Error minting NFT");
                
                return new OASISResult<string>
                {
                    Message = $"Error minting NFT: {ex.Message}",
                    IsError = true
                };
            }
        }

        /// <summary>
        /// Mint a group membership badge
        /// </summary>
        public async Task<OASISResult<string>> MintGroupBadgeAsync(
            string groupName,
            string memberName,
            string recipientWallet,
            Guid mintedByAvatarId,
            string imageUrl = null)
        {
            return await MintAchievementNFTAsync(
                title: $"{groupName} Member",
                description: $"Official member of {groupName} - {memberName}",
                recipientWallet: recipientWallet,
                mintedByAvatarId: mintedByAvatarId,
                symbol: "MEMBER",
                imageUrl: imageUrl ?? "https://via.placeholder.com/500?text=Member+Badge"
            );
        }

        /// <summary>
        /// Quick mint for testing - uses placeholder data
        /// </summary>
        public async Task<OASISResult<string>> MintTestNFTAsync(
            string title,
            string description,
            string recipientWallet,
            Guid mintedByAvatarId)
        {
            _logger?.LogInformation($"[NFTService] Minting TEST NFT: {title}");
            
            return await MintAchievementNFTAsync(
                title: title,
                description: description,
                recipientWallet: recipientWallet,
                mintedByAvatarId: mintedByAvatarId,
                symbol: "TEST",
                imageUrl: "https://via.placeholder.com/500/FF6B6B/FFFFFF?text=Test+NFT"
            );
        }
    }
}

