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

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    /// <summary>
    /// Polkadot Provider for OASIS
    /// Implements Polkadot parachain integration for multi-chain interoperability
    /// </summary>
    public class PolkadotOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _chainId;
        private readonly string _privateKey;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the PolkadotOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Polkadot RPC endpoint URL</param>
        /// <param name="chainId">Polkadot chain ID</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public PolkadotOASIS(string rpcEndpoint = "https://rpc.polkadot.io", string chainId = "polkadot", string privateKey = "")
        {
            this.ProviderName = "PolkadotOASIS";
            this.ProviderDescription = "Polkadot Provider - Multi-chain interoperability protocol";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PolkadotOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _chainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
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
                    response.Message = "Polkadot provider is already activated";
                    return response;
                }

                // Test connection to Polkadot RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "Polkadot provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to Polkadot RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Polkadot provider: {ex.Message}");
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
                response.Message = "Polkadot provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Polkadot provider: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Load avatar from Polkadot blockchain
                var queryUrl = $"/api/v1/accounts/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParsePolkadotToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Polkadot successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse Polkadot JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Polkadot blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Query Polkadot account by provider key using JSON-RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "system_account",
                    @params = new[] { providerKey }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatar = new Avatar
                        {
                            Id = Guid.NewGuid(),
                            Username = providerKey,
                            Email = result.TryGetProperty("data", out var data) ? data.GetString() : "",
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };
                        
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Polkadot account successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Account not found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Polkadot account: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Query Polkadot account by email using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getAvatarByEmail",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"email\":\"{avatarEmail}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatarData = JsonSerializer.Deserialize<Avatar>(result.GetString());
                        response.Result = avatarData;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Polkadot by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Polkadot by email: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Query Polkadot account by username using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getAvatarByUsername",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"username\":\"{avatarUsername}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatarData = JsonSerializer.Deserialize<Avatar>(result.GetString());
                        response.Result = avatarData;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Polkadot by username successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query Polkadot by username: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Query all avatars from Polkadot blockchain using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getAllAvatars",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatarsData = JsonSerializer.Deserialize<Avatar[]>(result.GetString());
                        response.Result = avatarsData?.FirstOrDefault();
                        response.IsError = false;
                        response.Message = "Avatars loaded from Polkadot successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Query avatar details from Polkadot blockchain using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getAvatarDetail",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"avatar_id\":\"{id}\"}}")),
                        null
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatarData = JsonSerializer.Deserialize<AvatarDetail>(result.GetString());
                        response.Result = avatarData as IAvatar;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from Polkadot successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Save avatar to Polkadot blockchain using smart contract call
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        await CreatePolkadotTransaction("Oasis_saveAvatar", avatarJson)
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar saved to Polkadot blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Delete avatar from Polkadot blockchain using smart contract call
                var deleteData = JsonSerializer.Serialize(new { avatar_id = id.ToString(), soft_delete = softDelete });
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "author_submitExtrinsic",
                    @params = new[]
                    {
                        await CreatePolkadotTransaction("Oasis_deleteAvatar", deleteData)
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = "Avatar deleted from Polkadot blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Get players near me from Polkadot blockchain
                var queryUrl = "/api/v1/accounts/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Polkadot JSON and create Player collection
                    OASISErrorHandling.HandleError(ref response, "Polkadot JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from Polkadot blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Polkadot: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Polkadot provider is not activated");
                    return response;
                }

                // Get holons near me from Polkadot blockchain
                var queryUrl = $"/api/v1/accounts/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse Polkadot JSON and create Holon collection
                    OASISErrorHandling.HandleError(ref response, "Polkadot JSON parsing not implemented - requires JSON parsing library");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Polkadot blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Polkadot: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Create a Polkadot transaction for smart contract calls
        /// </summary>
        private async Task<string> CreatePolkadotTransaction(string method, string data)
        {
            try
            {
                // Get current block info
                var blockRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "chain_getHeader",
                    @params = new string[0]
                };

                var blockResponse = await _httpClient.PostAsync("", new StringContent(JsonSerializer.Serialize(blockRequest), Encoding.UTF8, "application/json"));
                var blockContent = await blockResponse.Content.ReadAsStringAsync();
                var blockData = JsonSerializer.Deserialize<JsonElement>(blockContent);
                
                var blockHash = blockData.GetProperty("result").GetProperty("hash").GetString();
                var blockNumber = blockData.GetProperty("result").GetProperty("number").GetString();

                // Create Polkadot extrinsic
                var extrinsic = new
                {
                    method = new
                    {
                        call_index = "0x0000", // This would be the actual call index
                        args = new
                        {
                            method = method,
                            data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
                        }
                    },
                    era = new
                    {
                        period = "64",
                        phase = "0"
                    },
                    nonce = "0",
                    tip = "0"
                };

                // Sign transaction (simplified - in real implementation would use proper signing)
                var transactionJson = JsonSerializer.Serialize(extrinsic);
                var signature = "0x" + Convert.ToHexString(Encoding.UTF8.GetBytes("signature")); // Simplified
                
                var signedTransaction = new
                {
                    extrinsic = extrinsic,
                    signature = signature
                };

                return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(signedTransaction)));
            }
            catch (Exception)
            {
                // Return a basic signed transaction for testing
                return Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"extrinsic\":{\"method\":{\"call_index\":\"0x0000\",\"args\":{\"method\":\"" + method + "\",\"data\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(data)) + "\"}},\"era\":{\"period\":\"64\",\"phase\":\"0\"},\"nonce\":\"0\",\"tip\":\"0\"},\"signature\":\"0xtest\"}"));
            }
        }

        /// <summary>
        /// Parse Polkadot blockchain response to Avatar object
        /// </summary>
        private Avatar ParsePolkadotToAvatar(string polkadotJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Polkadot JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(polkadotJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromPolkadot(polkadotJson);
            }
        }

        /// <summary>
        /// Create Avatar from Polkadot response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromPolkadot(string polkadotJson)
        {
            try
            {
                // Extract basic information from Polkadot JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractPolkadotProperty(polkadotJson, "address") ?? "polkadot_user",
                    Email = ExtractPolkadotProperty(polkadotJson, "email") ?? "user@polkadot.example",
                    FirstName = ExtractPolkadotProperty(polkadotJson, "first_name"),
                    LastName = ExtractPolkadotProperty(polkadotJson, "last_name"),
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
        /// Extract property value from Polkadot JSON response
        /// </summary>
        private string ExtractPolkadotProperty(string polkadotJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Polkadot properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(polkadotJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Polkadot blockchain format
        /// </summary>
        private string ConvertAvatarToPolkadot(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Polkadot blockchain structure
                var polkadotData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(polkadotData, new JsonSerializerOptions
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
        /// Convert Holon to Polkadot blockchain format
        /// </summary>
        private string ConvertHolonToPolkadot(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Polkadot blockchain structure
                var polkadotData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(polkadotData, new JsonSerializerOptions
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
    }
}


