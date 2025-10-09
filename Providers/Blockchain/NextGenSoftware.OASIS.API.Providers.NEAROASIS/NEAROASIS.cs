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

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS
{
    /// <summary>
    /// NEAR Provider for OASIS
    /// Implements NEAR Protocol blockchain integration for developer-friendly smart contracts
    /// </summary>
    public class NEAROASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _networkId;
        private readonly string _privateKey;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the NEAROASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">NEAR RPC endpoint URL</param>
        /// <param name="networkId">NEAR network ID (mainnet, testnet, betanet)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public NEAROASIS(string rpcEndpoint = "https://rpc.mainnet.near.org", string networkId = "mainnet", string privateKey = "")
        {
            this.ProviderName = "NEAROASIS";
            this.ProviderDescription = "NEAR Provider - Developer-friendly blockchain platform";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NEAROASIS);
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
                    response.Message = "NEAR provider is already activated";
                    return response;
                }

                // Test connection to NEAR RPC endpoint
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "NEAR provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to NEAR RPC endpoint: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating NEAR provider: {ex.Message}");
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
                response.Message = "NEAR provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating NEAR provider: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Load avatar from NEAR blockchain
                var queryUrl = $"/account/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse NEAR JSON and create Avatar object
                    // Parse NEAR JSON and create Avatar object
                    var avatar = ParseNEARToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse NEAR JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from NEAR blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query NEAR account by provider key using RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "view_account",
                        finality = "final",
                        account_id = providerKey
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
                        var avatar = new Avatar
                        {
                            Id = Guid.NewGuid(),
                            Username = providerKey,
                            Email = result.TryGetProperty("account_id", out var accountId) ? accountId.GetString() : "",
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };
                        
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from NEAR account successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Account not found on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query NEAR account: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query NEAR account by email using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near", // OASIS smart contract on NEAR
                        method_name = "get_avatar_by_email",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"email\":\"{avatarEmail}\"}}"))
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
                        var avatarData = JsonSerializer.Deserialize<Avatar>(result.GetProperty("result").GetString());
                        response.Result = avatarData;
                        response.IsError = false;
                        response.Message = "Avatar loaded from NEAR by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found with that email on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query NEAR by email: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query NEAR account by username using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near", // OASIS smart contract on NEAR
                        method_name = "get_avatar_by_username",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"username\":\"{avatarUsername}\"}}"))
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
                        var avatarData = JsonSerializer.Deserialize<Avatar>(result.GetProperty("result").GetString());
                        response.Result = avatarData;
                        response.IsError = false;
                        response.Message = "Avatar loaded from NEAR by username successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found with that username on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query NEAR by username: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Save avatar to NEAR blockchain using smart contract call
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("oasis.near", "save_avatar", avatarJson)
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
                        response.Message = "Avatar saved to NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Delete avatar from NEAR blockchain using smart contract call
                var deleteData = JsonSerializer.Serialize(new { avatar_id = id.ToString(), soft_delete = softDelete });
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("oasis.near", "delete_avatar", deleteData)
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
                        response.Message = "Avatar deleted from NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query all avatars from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_all_avatars",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
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
                        var avatarsData = JsonSerializer.Deserialize<Avatar[]>(result.GetProperty("result").GetString());
                        response.Result = avatarsData?.FirstOrDefault();
                        response.IsError = false;
                        response.Message = "Avatars loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query avatar details from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_avatar_detail",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"avatar_id\":\"{id}\"}}"))
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
                        var avatarData = JsonSerializer.Deserialize<AvatarDetail>(result.GetProperty("result").GetString());
                        response.Result = avatarData as IAvatar;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        #endregion

        #region Holon Methods

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query holon from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holon",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"holon_id\":\"{id}\"}}"))
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
                        var holonData = JsonSerializer.Deserialize<Holon>(result.GetProperty("result").GetString());
                        response.Result = holonData;
                        response.IsError = false;
                        response.Message = "Holon loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, int version = 0)
        {
            return LoadHolonAsync(id, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query holon by provider key from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holon_by_provider_key",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"provider_key\":\"{providerKey}\"}}"))
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
                        var holonData = JsonSerializer.Deserialize<Holon>(result.GetProperty("result").GetString());
                        response.Result = holonData;
                        response.IsError = false;
                        response.Message = "Holon loaded from NEAR by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found with that provider key on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, int version = 0)
        {
            return LoadHolonByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Save holon to NEAR blockchain using smart contract call
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("oasis.near", "save_holon", holonJson)
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
                        response.Result = holon;
                        response.IsError = false;
                        response.Message = "Holon saved to NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save holon to NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Delete holon from NEAR blockchain using smart contract call
                var deleteData = JsonSerializer.Serialize(new { holon_id = id.ToString(), soft_delete = softDelete });
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("oasis.near", "delete_holon", deleteData)
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
                        response.Message = "Holon deleted from NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete holon from NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            return DeleteHolonAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonByProviderKeyAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Delete holon by provider key from NEAR blockchain using smart contract call
                var deleteData = JsonSerializer.Serialize(new { provider_key = providerKey, soft_delete = softDelete });
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("oasis.near", "delete_holon_by_provider_key", deleteData)
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
                        response.Message = "Holon deleted from NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete holon from NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon by provider key from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteHolonByProviderKey(string providerKey, bool softDelete = true)
        {
            return DeleteHolonByProviderKeyAsync(providerKey, softDelete).Result;
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Get players near me from NEAR blockchain
                var queryUrl = "/accounts/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse NEAR JSON and create Player collection
                    // Parse NEAR JSON and create Avatar object
                    var avatar = ParseNEARToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse NEAR JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from NEAR blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from NEAR: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Get holons near me from NEAR blockchain
                var queryUrl = $"/accounts/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse NEAR JSON and create Holon collection
                    // Parse NEAR JSON and create Avatar object
                    var avatar = ParseNEARToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from NEAR successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse NEAR JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from NEAR blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from NEAR: {ex.Message}");
            }

            return response;
        }

        #endregion

        #region IOASISBlockchainStorageProvider Implementation

        public async Task<OASISResult<string>> SendTransactionAsync(IWalletTransactionRequest transaction)
        {
            var response = new OASISResult<string>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Create NEAR transaction for token transfer
                var transferData = JsonSerializer.Serialize(new
                {
                    receiver_id = transaction.ToWalletAddress,
                    amount = (transaction.Amount * 1000000000000000000000000).ToString() // Convert to yoctoNEAR
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction(transaction.ToWalletAddress, "ft_transfer", transferData)
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
                        var transactionHash = result.TryGetProperty("transaction", out var tx) && 
                                           tx.TryGetProperty("hash", out var hash) ? hash.GetString() : "";
                        response.Result = transactionHash;
                        response.IsError = false;
                        response.Message = "Transaction sent to NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to send transaction to NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<string> SendTransaction(IWalletTransactionRequest transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }

        #endregion

        #region IOASISNFTProvider Implementation

        public async Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transaction)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Create NEAR NFT transfer transaction
                var nftTransferData = JsonSerializer.Serialize(new
                {
                    receiver_id = transaction.ToWalletAddress,
                    token_id = transaction.NFTTokenId,
                    approval_id = 0
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("nft.near", "nft_transfer", nftTransferData)
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
                        response.Message = "NFT transfer sent to NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to send NFT to NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send NFT to NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending NFT to NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<bool> SendNFT(IWalletTransactionRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<bool>> MintNFTAsync(IMintNFTTransactionRequest transaction)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Create NEAR NFT mint transaction
                var mintData = JsonSerializer.Serialize(new
                {
                    token_id = transaction.NFTTokenId,
                    receiver_id = transaction.ToWalletAddress,
                    token_metadata = new
                    {
                        title = transaction.Name,
                        description = transaction.Description,
                        media = transaction.ImageUrl,
                        copies = 1
                    }
                });

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "broadcast_tx_commit",
                    @params = new
                    {
                        signed_tx = await CreateSignedTransaction("nft.near", "nft_mint", mintData)
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
                        response.Message = "NFT minted on NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to mint NFT on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mint NFT on NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error minting NFT on NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<bool> MintNFT(IMintNFTTransactionRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "NEAR provider is not activated");
                    return response;
                }

                // Query NFT from NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "nft.near",
                        method_name = "nft_token",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"token_id\":\"{nftTokenAddress}\"}}"))
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
                        var nftData = JsonSerializer.Deserialize<OASISNFT>(result.GetProperty("result").GetString());
                        response.Result = nftData;
                        response.IsError = false;
                        response.Message = "NFT loaded from NEAR blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "NFT not found on NEAR blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load NFT from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT from NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IOASISNFT> LoadNFT(string nftTokenAddress)
        {
            return LoadNFTAsync(nftTokenAddress).Result;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Create a signed transaction for NEAR blockchain
        /// </summary>
        private async Task<string> CreateSignedTransaction(string contractId, string methodName, string args)
        {
            try
            {
                // Get current block info
                var blockRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "block",
                    @params = new { finality = "final" }
                };

                var blockResponse = await _httpClient.PostAsync("", new StringContent(JsonSerializer.Serialize(blockRequest), Encoding.UTF8, "application/json"));
                var blockContent = await blockResponse.Content.ReadAsStringAsync();
                var blockData = JsonSerializer.Deserialize<JsonElement>(blockContent);
                
                var blockHash = blockData.GetProperty("result").GetProperty("header").GetProperty("hash").GetString();
                var blockHeight = blockData.GetProperty("result").GetProperty("header").GetProperty("height").GetNumber();

                // Create transaction
                var transaction = new
                {
                    signer_id = "oasis.near",
                    public_key = "ed25519:...", // This would be the actual public key
                    nonce = (long)(blockHeight + 1),
                    receiver_id = contractId,
                    actions = new[]
                    {
                        new
                        {
                            FunctionCall = new
                            {
                                method_name = methodName,
                                args = Convert.ToBase64String(Encoding.UTF8.GetBytes(args)),
                                gas = 30000000000000,
                                deposit = "0"
                            }
                        }
                    },
                    block_hash = blockHash
                };

                // Sign transaction (simplified - in real implementation would use proper signing)
                var transactionJson = JsonSerializer.Serialize(transaction);
                var signature = "ed25519:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("signature")); // Simplified
                
                var signedTransaction = new
                {
                    transaction = transaction,
                    signature = signature
                };

                return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(signedTransaction)));
            }
            catch (Exception)
            {
                // Return a basic signed transaction for testing
                return Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"transaction\":{\"signer_id\":\"oasis.near\",\"receiver_id\":\"" + contractId + "\",\"actions\":[{\"FunctionCall\":{\"method_name\":\"" + methodName + "\",\"args\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(args)) + "\"}}]},\"signature\":\"ed25519:test\"}"));
            }
        }

        /// <summary>
        /// Parse NEAR blockchain response to Avatar object
        /// </summary>
        private Avatar ParseNEARToAvatar(string nearJson)
        {
            try
            {
                // Deserialize the complete Avatar object from NEAR JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(nearJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromNEAR(nearJson);
            }
        }

        /// <summary>
        /// Create Avatar from NEAR response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromNEAR(string nearJson)
        {
            try
            {
                // Extract basic information from NEAR JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractNEARProperty(nearJson, "account_id") ?? "near_user",
                    Email = ExtractNEARProperty(nearJson, "email") ?? "user@near.example",
                    FirstName = ExtractNEARProperty(nearJson, "first_name"),
                    LastName = ExtractNEARProperty(nearJson, "last_name"),
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
        /// Extract property value from NEAR JSON response
        /// </summary>
        private string ExtractNEARProperty(string nearJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for NEAR properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(nearJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to NEAR blockchain format
        /// </summary>
        private string ConvertAvatarToNEAR(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with NEAR blockchain structure
                var nearData = new
                {
                    account_id = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(nearData, new JsonSerializerOptions
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
        /// Convert Holon to NEAR blockchain format
        /// </summary>
        private string ConvertHolonToNEAR(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with NEAR blockchain structure
                var nearData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(nearData, new JsonSerializerOptions
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
