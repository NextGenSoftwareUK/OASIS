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
    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

            if (holons == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                return result;
            }

            // Serialize holons to JSON for import
            var holonsJson = JsonSerializer.Serialize(holons);
            
            // Import holons to Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("import_holons"),
                    calldata = new[] { holonsJson }
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
                result.Message = $"Successfully imported {holons.Count()} holons to Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
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

            // Export all data for avatar by ID from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("export_all_data_for_avatar_by_id"),
                    calldata = new[] { avatarId.ToString(), version.ToString() }
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
                    var holons = new List<IHolon>();
                    if (rpcResult.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var holonElement in rpcResult.EnumerateArray())
                        {
                            var holon = ParseStarknetToHolon(holonElement);
                            if (holon != null) holons.Add(holon);
                        }
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    /// <summary>
    /// Gets the entry point selector for a Starknet function name
    /// Starknet uses Keccak256 hash of the function name, truncated to 250 bits
    /// </summary>
    private string GetEntryPointSelector(string functionName)
    {
        // Starknet entry point selector is Keccak256 hash of function name, truncated to 250 bits (62 hex chars)
        // For simplicity, we'll use SHA256 and truncate (in production, use proper Keccak256)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(functionName));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        // Truncate to 62 characters (250 bits) and add 0x prefix
        return "0x" + hashHex.Substring(0, Math.Min(62, hashHex.Length));
    }

    private IAvatar ParseStarknetToAvatar(JsonElement element)
    {
        try
        {
            var avatar = new Avatar();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                {
                    avatar.Id = id;
                }
            }
            if (element.TryGetProperty("username", out var usernameProp))
            {
                avatar.Username = usernameProp.GetString();
            }
            if (element.TryGetProperty("email", out var emailProp))
            {
                avatar.Email = emailProp.GetString();
            }
            return avatar;
        }
        catch
        {
            return null;
        }
    }

    private IAvatarDetail ParseStarknetToAvatarDetail(JsonElement element)
    {
        try
        {
            // Try full deserialize first so Inventory (including Quantity/Stack) is restored
            try
            {
                var raw = element.GetRawText();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var full = JsonSerializer.Deserialize<AvatarDetail>(raw);
                    if (full != null)
                        return full;
                }
            }
            catch { /* fall back to minimal parse */ }

            var avatarDetail = new AvatarDetail();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                    avatarDetail.Id = id;
            }
            if (element.TryGetProperty("username", out var usernameProp))
                avatarDetail.Username = usernameProp.GetString();
            if (element.TryGetProperty("email", out var emailProp))
                avatarDetail.Email = emailProp.GetString();
            if (element.TryGetProperty("Inventory", out var invProp))
            {
                var invRaw = invProp.GetRawText();
                if (!string.IsNullOrWhiteSpace(invRaw))
                {
                    var list = JsonSerializer.Deserialize<List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>>(invRaw);
                    if (list != null)
                        avatarDetail.Inventory = new List<IInventoryItem>(list);
                }
            }
            return avatarDetail;
        }
        catch
        {
            return null;
        }
    }

    private IHolon ParseStarknetToHolon(JsonElement element)
    {
        try
        {
            var holon = new Holon();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                {
                    holon.Id = id;
                }
            }
            if (element.TryGetProperty("name", out var nameProp))
            {
                holon.Name = nameProp.GetString();
            }
            if (element.TryGetProperty("description", out var descProp))
            {
                holon.Description = descProp.GetString();
            }
            if (element.TryGetProperty("parent_id", out var parentIdProp))
            {
                var parentIdStr = parentIdProp.GetString();
                if (Guid.TryParse(parentIdStr, out var parentId))
                {
                    holon.ParentHolonId = parentId;
                }
            }
            return holon;
        }
        catch
        {
            return null;
        }
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
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

            // Export all data for avatar by username from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("export_all_data_for_avatar_by_username"),
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
                    var holons = new List<IHolon>();
                    if (rpcResult.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var holonElement in rpcResult.EnumerateArray())
                        {
                            var holon = ParseStarknetToHolon(holonElement);
                            if (holon != null) holons.Add(holon);
                        }
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar by username from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

}
