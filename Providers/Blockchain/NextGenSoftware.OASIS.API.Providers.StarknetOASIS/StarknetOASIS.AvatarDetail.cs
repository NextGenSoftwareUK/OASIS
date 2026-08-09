using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS
{
    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar detail by ID from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_avatar_detail_by_id"),
                    calldata = new[] { id.ToString(), version.ToString() }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("result", out var rpcResult))
                {
                    var avatarDetail = ParseStarknetToAvatarDetail(rpcResult);
                    if (avatarDetail != null)
                    {
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Successfully loaded avatar detail from Starknet";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail from Starknet RPC response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse Starknet RPC response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar detail by email from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_avatar_detail_by_email"),
                    calldata = new[] { avatarEmail, version.ToString() }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("result", out var rpcResult))
                {
                    var avatarDetail = ParseStarknetToAvatarDetail(rpcResult);
                    if (avatarDetail != null)
                    {
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Successfully loaded avatar detail by email from Starknet";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail from Starknet RPC response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse Starknet RPC response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar detail by username from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_avatar_detail_by_username"),
                    calldata = new[] { avatarUsername, version.ToString() }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("result", out var rpcResult))
                {
                    var avatarDetail = ParseStarknetToAvatarDetail(rpcResult);
                    if (avatarDetail != null)
                    {
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Successfully loaded avatar detail by username from Starknet";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail from Starknet RPC response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse Starknet RPC response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query all avatar details from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_all_avatar_details"),
                    calldata = new[] { version.ToString() }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("result", out var rpcResult))
                {
                    var avatarDetails = new List<IAvatarDetail>();
                    if (rpcResult.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var detailElement in rpcResult.EnumerateArray())
                        {
                            var avatarDetail = ParseStarknetToAvatarDetail(detailElement);
                            if (avatarDetail != null)
                            {
                                avatarDetails.Add(avatarDetail);
                            }
                        }
                    }
                    
                    result.Result = avatarDetails;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Starknet";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse Starknet RPC response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }


    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            if (Avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                return result;
            }

            // Serialize avatar detail to JSON for storage
            var avatarDetailJson = JsonSerializer.Serialize(Avatar);
            
            // Save avatar detail to Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("save_avatar_detail"),
                    calldata = new[] { avatarDetailJson }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                result.Result = Avatar;
                result.IsError = false;
                result.Message = "Successfully saved avatar detail to Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
    {
        return SaveAvatarDetailAsync(Avatar).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_avatar"),
                    calldata = new[] { id.ToString(), softDelete ? "1" : "0" }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully deleted avatar from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar by provider key from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_avatar_by_provider_key"),
                    calldata = new[] { providerKey, softDelete ? "1" : "0" }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully deleted avatar by provider key from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar by email from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_avatar_by_email"),
                    calldata = new[] { avatarEmail, softDelete ? "1" : "0" }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully deleted avatar by email from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }

            // Delete avatar by username from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_avatar_by_username"),
                    calldata = new[] { avatarUsername, softDelete ? "1" : "0" }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully deleted avatar by username from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

}
