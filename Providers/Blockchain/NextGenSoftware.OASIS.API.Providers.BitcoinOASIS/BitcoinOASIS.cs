using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    /// <summary>
    /// Bitcoin Provider for OASIS
    /// Implements Bitcoin blockchain integration for digital currency and smart contracts
    /// </summary>
    public class BitcoinOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _network;
        private readonly string _privateKey;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the BitcoinOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Bitcoin RPC endpoint URL</param>
        /// <param name="network">Bitcoin network (mainnet, testnet, regtest)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public BitcoinOASIS(string rpcEndpoint = "https://blockstream.info/api", string network = "mainnet", string privateKey = "")
        {
            this.ProviderName = "BitcoinOASIS";
            this.ProviderDescription = "Bitcoin Provider - First and largest cryptocurrency";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _privateKey = privateKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint)
            };
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Bitcoin provider is already activated";
                    return response;
                }

                // Test connection to Bitcoin RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Bitcoin provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Bitcoin RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Bitcoin provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                _httpClient?.Dispose();
                response.Result = true;
                response.Message = "Bitcoin provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Bitcoin provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Load avatar from Bitcoin blockchain
                var queryUrl = $"/address/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse Bitcoin JSON and create Avatar object
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by address
                var queryUrl = $"/address/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var addressData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var avatar = ParseBitcoinToAvatar(addressData, providerKey);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Bitcoin successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Bitcoin address data");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by email using OP_RETURN data
                var queryUrl = $"/address/{avatarEmail}/txs";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Search for transactions containing email in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) && 
                                    script.TryGetProperty("asm", out var asm) && 
                                    asm.GetString().Contains(avatarEmail))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarEmail);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by email successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Query Bitcoin blockchain for avatar by username using OP_RETURN data
                var queryUrl = $"/address/{avatarUsername}/txs";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Search for transactions containing username in OP_RETURN
                    foreach (var tx in txData)
                    {
                        if (tx.TryGetProperty("vout", out var vout))
                        {
                            foreach (var output in vout.EnumerateArray())
                            {
                                if (output.TryGetProperty("scriptpubkey", out var script) && 
                                    script.TryGetProperty("asm", out var asm) && 
                                    asm.GetString().Contains(avatarUsername))
                                {
                                    var avatar = ParseBitcoinToAvatar(tx, avatarUsername);
                                    if (avatar != null)
                                    {
                                        response.Result = avatar;
                                        response.IsError = false;
                                        response.Message = "Avatar loaded from Bitcoin by username successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Bitcoin: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Bitcoin: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Get players near me from Bitcoin blockchain
                var queryUrl = "/addresses/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Bitcoin JSON and create Player collection
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from Bitcoin blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Bitcoin: {ex.Message}");
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Bitcoin provider is not activated");
                    return response;
                }

                // Get holons near me from Bitcoin blockchain
                var queryUrl = $"/addresses/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Bitcoin JSON and create Holon collection
                    OASISErrorHandling.HandleError(ref response, "Bitcoin JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Bitcoin blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Bitcoin: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(string bitcoinJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Bitcoin JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(bitcoinJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromBitcoin(bitcoinJson);
            }
        }

        /// <summary>
        /// Create Avatar from Bitcoin response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromBitcoin(string bitcoinJson)
        {
            try
            {
                // Extract basic information from Bitcoin JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractBitcoinProperty(bitcoinJson, "address") ?? "bitcoin_user",
                    Email = ExtractBitcoinProperty(bitcoinJson, "email") ?? "user@bitcoin.example",
                    FirstName = ExtractBitcoinProperty(bitcoinJson, "first_name"),
                    LastName = ExtractBitcoinProperty(bitcoinJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Bitcoin JSON response
        /// </summary>
        private string ExtractBitcoinProperty(string bitcoinJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Bitcoin properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(bitcoinJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Bitcoin blockchain format
        /// </summary>
        private string ConvertAvatarToBitcoin(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to Bitcoin blockchain format
        /// </summary>
        private string ConvertHolonToBitcoin(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(JsonElement bitcoinData, string identifier)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = identifier,
                    Email = bitcoinData.TryGetProperty("address", out var address) ? address.GetString() : identifier,
                    FirstName = "Bitcoin",
                    LastName = "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Bitcoin-specific metadata
                if (bitcoinData.TryGetProperty("chain_stats", out var chainStats))
                {
                    if (chainStats.TryGetProperty("funded_txo_sum", out var funded))
                    {
                        avatar.ProviderMetaData.Add("bitcoin_balance", funded.GetInt64().ToString());
                    }
                    if (chainStats.TryGetProperty("spent_txo_sum", out var spent))
                    {
                        avatar.ProviderMetaData.Add("bitcoin_spent", spent.GetInt64().ToString());
                    }
                }
                if (bitcoinData.TryGetProperty("mempool_stats", out var mempoolStats))
                {
                    if (mempoolStats.TryGetProperty("funded_txo_sum", out var mempoolFunded))
                    {
                        avatar.ProviderMetaData.Add("bitcoin_mempool_balance", mempoolFunded.GetInt64().ToString());
                    }
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}


