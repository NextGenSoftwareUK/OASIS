using System;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS
{
    public partial class NEAROASIS
    {
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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
                        response.Result = avatarData;
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }



        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadHolonByProviderKeyAsync(providerKey, version);
        }

        public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, int version = 0)
        {
            return LoadHolonByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true)
        {
            return await SaveHolonAsync(holon);
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var deleteResult = await DeleteHolonByIdAsync(id, true);
            return new OASISResult<IHolon> { IsError = deleteResult.IsError, Message = deleteResult.Message };
        }

        public async Task<OASISResult<bool>> DeleteHolonByIdAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            return DeleteHolonByIdAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var deleteResult = await DeleteHolonByProviderKeyAsync(providerKey, true);
            return new OASISResult<IHolon> { IsError = deleteResult.IsError, Message = deleteResult.Message };
        }

        public async Task<OASISResult<bool>> DeleteHolonByProviderKeyAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
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

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public OASISResult<bool> DeleteHolonByProviderKey(string providerKey, bool softDelete = true)
        {
            return DeleteHolonByProviderKeyAsync(providerKey, softDelete).Result;
        }

        // Additional missing abstract methods
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query all holons of specific type from NEAR smart contract
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
                        method_name = "get_all_holons_by_type",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"holon_type\":\"{holonType}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenRecursiveDepth).Result;
        }

    }
}
