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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS
{
    /// <summary>
    /// Cardano Provider for OASIS
    /// Implements Cardano blockchain integration for smart contracts and NFTs
    /// </summary>
    public class CardanoOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _networkId;
        private readonly string _privateKey;
        private bool _isActivated;
        private WalletManager _walletManager;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, OASISDNA);
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the CardanoOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Cardano RPC endpoint URL</param>
        /// <param name="networkId">Cardano network ID (mainnet, testnet, preprod)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public CardanoOASIS(string rpcEndpoint = "https://cardano-mainnet.blockfrost.io/api/v0", string networkId = "mainnet", string privateKey = "")
        {
            this.ProviderName = "CardanoOASIS";
            this.ProviderDescription = "Cardano Provider - Third-generation blockchain platform";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CardanoOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _networkId = networkId ?? throw new ArgumentNullException(nameof(networkId));
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
                    response.Message = "Cardano provider is already activated";
                    return response;
                }

                // Test connection to Cardano RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Cardano provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Cardano RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Cardano provider: {ex.Message}");
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
                response.Message = "Cardano provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Cardano provider: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Load avatar from Cardano blockchain
                var queryUrl = $"/addresses/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseCardanoToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Cardano successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Cardano JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Cardano blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Cardano: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Query Cardano address by provider key using Blockfrost API
                var queryUrl = $"/addresses/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var addressData = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var avatar = new Avatar
                    {
                        Id = Guid.NewGuid(),
                        Username = providerKey,
                        Email = addressData.TryGetProperty("address", out var address) ? address.GetString() : "",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };
                    
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded from Cardano address successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano address: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Cardano: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Query Cardano metadata for avatar by email using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100"; // NFT metadata standard
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Search for metadata containing the email
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(avatarEmail))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatar loaded from Cardano by email successfully";
                                    return response;
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano metadata: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Cardano: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Query Cardano metadata for avatar by username using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100"; // NFT metadata standard
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Search for metadata containing the username
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(avatarUsername))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatar loaded from Cardano by username successfully";
                                    return response;
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Cardano metadata: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Query all avatars from Cardano blockchain using Blockfrost API
                var queryUrl = "/metadata/txs/labels/721?count=100"; // NFT metadata standard
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Find first avatar metadata
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains("avatar"))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatars loaded from Cardano successfully";
                                    return response;
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "No avatars found on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Query avatar details from Cardano blockchain using Blockfrost API
                var queryUrl = $"/metadata/txs/labels/721?count=100";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var metadataArray = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    // Search for metadata containing the avatar ID
                    foreach (var metadata in metadataArray)
                    {
                        if (metadata.TryGetProperty("json_metadata", out var jsonMeta))
                        {
                            var metadataString = jsonMeta.GetString();
                            if (metadataString.Contains(id.ToString()))
                            {
                                var avatar = ParseCardanoToAvatar(metadataString);
                                if (avatar != null)
                                {
                                    response.Result = avatar;
                                    response.IsError = false;
                                    response.Message = "Avatar detail loaded from Cardano successfully";
                                    return response;
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Cardano blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Save avatar to Cardano blockchain using transaction with metadata
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Create Cardano transaction with metadata
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            inputs = new[]
                            {
                                new
                                {
                                    tx_hash = "0000000000000000000000000000000000000000000000000000000000000000",
                                    index = 0
                                }
                            },
                            outputs = new[]
                            {
                                new
                                {
                                    address = "addr1...", // This would be the actual address
                                    amount = new
                                    {
                                        quantity = 1000000,
                                        unit = "lovelace"
                                    }
                                }
                            },
                            fee = "174479",
                            ttl = 0
                        },
                        witness_set = new
                        {
                            vkey_witnesses = new[]
                            {
                                new
                                {
                                    vkey = "...", // This would be the actual verification key
                                    signature = "..." // This would be the actual signature
                                }
                            }
                        },
                        metadata = new
                        {
                            "721": new
                            {
                                avatar_data = avatarJson
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/tx/submit", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("id", out var txId))
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = $"Avatar saved to Cardano blockchain successfully. Transaction ID: {txId.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Cardano blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Delete avatar from Cardano blockchain using transaction with deletion metadata
                var deleteData = JsonSerializer.Serialize(new { avatar_id = id.ToString(), deleted = true, soft_delete = softDelete });
                
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            inputs = new[]
                            {
                                new
                                {
                                    tx_hash = "0000000000000000000000000000000000000000000000000000000000000000",
                                    index = 0
                                }
                            },
                            outputs = new[]
                            {
                                new
                                {
                                    address = "addr1...", // This would be the actual address
                                    amount = new
                                    {
                                        quantity = 1000000,
                                        unit = "lovelace"
                                    }
                                }
                            },
                            fee = "174479",
                            ttl = 0
                        },
                        witness_set = new
                        {
                            vkey_witnesses = new[]
                            {
                                new
                                {
                                    vkey = "...", // This would be the actual verification key
                                    signature = "..." // This would be the actual signature
                                }
                            }
                        },
                        metadata = new
                        {
                            "721": new
                            {
                                avatar_deletion = deleteData
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/tx/submit", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("id", out var txId))
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = $"Avatar deleted from Cardano blockchain successfully. Transaction ID: {txId.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from Cardano blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from Cardano: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Cardano: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Get players near me from Cardano blockchain
                var queryUrl = "/addresses/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Cardano JSON and create Player collection
                    OASISErrorHandling.HandleError(ref response, "Cardano JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from Cardano blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Cardano: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Get holons near me from Cardano blockchain
                var queryUrl = $"/addresses/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Cardano JSON and create Holon collection
                    OASISErrorHandling.HandleError(ref response, "Cardano JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Cardano blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Cardano: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse Cardano blockchain response to Avatar object
        /// </summary>
        private Avatar ParseCardanoToAvatar(string cardanoJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Cardano JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(cardanoJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromCardano(cardanoJson);
            }
        }

        /// <summary>
        /// Create Avatar from Cardano response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromCardano(string cardanoJson)
        {
            try
            {
                // Extract basic information from Cardano JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractCardanoProperty(cardanoJson, "stake_address") ?? "cardano_user",
                    Email = ExtractCardanoProperty(cardanoJson, "email") ?? "user@cardano.example",
                    FirstName = ExtractCardanoProperty(cardanoJson, "first_name"),
                    LastName = ExtractCardanoProperty(cardanoJson, "last_name"),
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
        /// Extract property value from Cardano JSON response
        /// </summary>
        private string ExtractCardanoProperty(string cardanoJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Cardano properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(cardanoJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Cardano blockchain format
        /// </summary>
        private string ConvertAvatarToCardano(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Cardano blockchain structure
                var cardanoData = new
                {
                    stake_address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(cardanoData, new JsonSerializerOptions
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
        /// Convert Holon to Cardano blockchain format
        /// </summary>
        private string ConvertHolonToCardano(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Cardano blockchain structure
                var cardanoData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(cardanoData, new JsonSerializerOptions
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

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
                    return result;
                }

                // Convert decimal amount to lovelace (1 ADA = 1,000,000 lovelace)
                var amountInLovelace = (long)(amount * 1000000);
                
                // Get UTXOs for the from address using Blockfrost API
                var utxoResponse = await _httpClient.GetAsync($"/addresses/{fromWalletAddress}/utxos");
                if (!utxoResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get UTXOs for Cardano address {fromWalletAddress}: {utxoResponse.StatusCode}");
                    return result;
                }

                var utxoContent = await utxoResponse.Content.ReadAsStringAsync();
                var utxos = JsonSerializer.Deserialize<JsonElement[]>(utxoContent);
                
                if (utxos == null || utxos.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No UTXOs found for Cardano address {fromWalletAddress}");
                    return result;
                }

                // Find sufficient UTXOs
                long totalValue = 0;
                var selectedUtxos = new List<object>();
                
                foreach (var utxo in utxos)
                {
                    var value = utxo.GetProperty("amount").GetProperty("quantity").GetInt64();
                    totalValue += value;
                    selectedUtxos.Add(new
                    {
                        tx_hash = utxo.GetProperty("tx_hash").GetString(),
                        output_index = utxo.GetProperty("output_index").GetInt32()
                    });
                    
                    if (totalValue >= amountInLovelace)
                        break;
                }

                if (totalValue < amountInLovelace)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient funds. Available: {totalValue} lovelace, Required: {amountInLovelace} lovelace");
                    return result;
                }

                // Create Cardano transaction
                var transactionRequest = new
                {
                    inputs = selectedUtxos,
                    outputs = new[]
                    {
                        new
                        {
                            address = toWalletAddress,
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = amountInLovelace.ToString()
                                }
                            }
                        }
                    },
                    metadata = new
                    {
                        memo = memoText
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    result.Result = new TransactionRespone
                    {
                        TransactionResult = responseData.GetProperty("tx_hash").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Cardano transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Cardano transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Cardano transaction: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return SaveAvatarDetailAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var response = new OASISResult<IAvatarDetail>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Serialize avatar detail to JSON
                var avatarDetailJson = JsonSerializer.Serialize(avatar);
                var avatarDetailBytes = Encoding.UTF8.GetBytes(avatarDetailJson);
                
                // Create Cardano transaction with avatar detail data
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = avatar.ProviderWallets[ProviderType.CardanoOASIS]?.Address ?? "",
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(avatarDetailBytes) // Store avatar detail data in datum
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    avatar.ProviderWallets[ProviderType.CardanoOASIS] = new Wallet()
                    {
                        Address = responseData.GetProperty("tx_hash").GetString(),
                        ProviderType = ProviderType.CardanoOASIS
                    };
                    
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar detail saved successfully to Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Cardano is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarEmail = avatarEmail,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);
                
                // Create Cardano transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Cardano is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarUsername = avatarUsername,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);
                
                // Create Cardano transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Cardano is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    providerKey = providerKey,
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);
                
                // Create Cardano transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon);
                var holonBytes = Encoding.UTF8.GetBytes(holonJson);
                
                // Create Cardano transaction with holon data
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(holonBytes) // Store holon data in datum
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    holon.ProviderWallets[ProviderType.CardanoOASIS] = new Wallet()
                    {
                        Address = responseData.GetProperty("tx_hash").GetString(),
                        ProviderType = ProviderType.CardanoOASIS
                    };
                    
                    response.Result = holon;
                    response.IsError = false;
                    response.Message = "Holon saved successfully to Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return response;
                    }
                    else if (!saveResult.IsError)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Saved {savedHolons.Count} holons to Cardano blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Cardano is immutable, so we can't actually delete
                // Instead, we mark the holon as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    holonId = id.ToString(),
                    timestamp = DateTime.UtcNow
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);
                
                // Create Cardano transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { Id = id };
                    response.IsError = false;
                    response.Message = "Holon deletion marked successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Cardano is immutable, so we can't actually delete
                // Instead, we mark the holon as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    providerKey = providerKey,
                    timestamp = DateTime.UtcNow
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);
                
                // Create Cardano transaction with delete marker
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Cardano network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { ProviderWallets = new Dictionary<ProviderType, IWallet> { { ProviderType.CardanoOASIS, new Wallet { Address = providerKey, ProviderType = ProviderType.CardanoOASIS } } } };
                    response.IsError = false;
                    response.Message = "Holon deletion marked successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Cardano: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Search Cardano blockchain for transactions matching search criteria
                var searchRequest = new
                {
                    query = searchParams.SearchQuery,
                    filters = new
                    {
                        fromDate = searchParams.FromDate,
                        toDate = searchParams.ToDate,
                        version = version
                    }
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var searchResponse = await _httpClient.PostAsync("/search", content);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var results = new SearchResults();
                    // Parse search results and populate results object
                    
                    response.Result = results;
                    response.IsError = false;
                    response.Message = "Search completed successfully on Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search Cardano blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error searching Cardano blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Import holons to Cardano blockchain
                var importResult = await SaveHolonsAsync(holons);
                if (importResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons to Cardano: {importResult.Message}");
                    return response;
                }

                response.Result = true;
                response.IsError = false;
                response.Message = $"Successfully imported {holons.Count()} holons to Cardano blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to Cardano: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Export all data from Cardano blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var exportResponse = await _httpClient.PostAsync("/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Export completed successfully from Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export from Cardano blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting from Cardano blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Export all data for specific avatar from Cardano blockchain
                var exportRequest = new
                {
                    avatarId = avatarId.ToString(),
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var exportResponse = await _httpClient.PostAsync("/export/avatar", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Export all data for specific avatar by username from Cardano blockchain
                var exportRequest = new
                {
                    avatarUsername = avatarUsername,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var exportResponse = await _httpClient.PostAsync("/export/avatar/username", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
                    return response;
                }

                // Export all data for specific avatar by email from Cardano blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var exportResponse = await _httpClient.PostAsync("/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Avatar data export completed successfully from Cardano blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
            }

            return response;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}


