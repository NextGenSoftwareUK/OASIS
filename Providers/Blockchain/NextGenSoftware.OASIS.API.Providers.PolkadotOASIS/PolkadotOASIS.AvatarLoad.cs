using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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
                            Id = CreateDeterministicGuid($"{ProviderType.Value}:{providerKey}"),
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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
                        response.Result = avatarsData?.Cast<IAvatar>();
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

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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
                        response.Result = avatarData;
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

    }
}
