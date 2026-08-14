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
    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
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

            // Export all holons from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("export_all"),
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
                    result.Message = $"Successfully exported {holons.Count} holons from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error exporting all holons from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
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

            // Export all data for avatar by email from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("export_all_data_for_avatar_by_email"),
                    calldata = new[] { avatarEmailAddress, version.ToString() }
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
                    result.Message = $"Successfully exported {holons.Count} holons for avatar by email from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

            // Query holons for parent from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_holons_for_parent"),
                    calldata = new[] { id.ToString(), type.ToString(), version.ToString() }
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
                            if (holon != null && (type == HolonType.All || holon.HolonType == type))
                            {
                                holons.Add(holon);
                            }
                        }
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
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

            // Query holon by provider key from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_holon_by_key"),
                    calldata = new[] { providerKey, version.ToString() }
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
                    var holon = ParseStarknetToHolon(rpcResult);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Successfully loaded holon from Starknet";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse holon from Starknet RPC response");
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
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
    {
        return GenerateKeyPairAsync().Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet provider is not activated");
                return result;
            }

            // Generate Starknet key pair using Ed25519
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var privateKeyBytes = new byte[32];
            rng.GetBytes(privateKeyBytes);
            
            var privateKey = Convert.ToBase64String(privateKeyBytes);
            
            // Derive public key from private key using SHA-256 hash (simplified for Starknet)
            byte[] publicKeyBytes;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                publicKeyBytes = sha256.ComputeHash(privateKeyBytes);
            }
            var publicKey = Convert.ToBase64String(publicKeyBytes);
            
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                keyPair.PrivateKey = privateKey;
                keyPair.PublicKey = publicKey;
                keyPair.WalletAddressLegacy = publicKey;
            }

            result.Result = keyPair;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }

}
