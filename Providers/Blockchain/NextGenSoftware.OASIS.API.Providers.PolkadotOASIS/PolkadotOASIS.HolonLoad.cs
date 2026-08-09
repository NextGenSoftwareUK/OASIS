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
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Query holons for parent from Polkadot blockchain using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getHolonsForParent",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"parent_id\":\"{id}\",\"holon_type\":\"{type}\"}}")),
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
                        var holonsData = JsonSerializer.Deserialize<Holon[]>(result.GetString());
                        var holons = new List<IHolon>();
                        if (holonsData != null)
                        {
                            foreach (var holon in holonsData)
                            {
                                if (type == HolonType.All || holon.HolonType == type)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {holons.Count} holons for parent from Polkadot";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found for parent on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // First load the parent holon to get its ID
                var parentResult = LoadHolonAsync(providerKey, false, continueOnError, 0, false, false, 0).Result;
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Parent holon with provider key {providerKey} not found");
                    return response;
                }

                // Then load holons for parent using the ID
                return LoadHolonsForParent(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, continueOnError, 0, false, false, 0);
            if (parentResult.IsError || parentResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Parent holon with provider key {providerKey} not found");
                return response;
            }

            // Then load holons for parent using the ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - delegate to ProviderManager as fallback
                    return await HolonManager.Instance.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, curentChildDepth, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                }

                // Query holons by metadata from Polkadot blockchain using smart contract call
                var metadataJson = JsonSerializer.Serialize(metaKeyValuePairs);
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "state_call",
                    @params = new[]
                    {
                        "Oasis_getHolonsByMetaData",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"metadata\":{metadataJson},\"match_mode\":\"{metaKeyValuePairMatchMode}\",\"holon_type\":\"{type}\"}}")),
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

                    if (rpcResponse.TryGetProperty("result", out var result) && !string.IsNullOrEmpty(result.GetString()))
                    {
                        var holonsData = ParsePolkadotToHolons(result.GetString());
                        response.Result = holonsData;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {holonsData.Count()} holons by metadata from Polkadot";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found with matching metadata on Polkadot blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata from Polkadot: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllHolons is not supported by Polkadot provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolons: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - query chain state using Substrate RPC
                    // Use state_queryStorage to query all holons stored on-chain
                    var queryRequest = new
                    {
                        id = 1,
                        jsonrpc = "2.0",
                        method = "state_queryStorage",
                        @params = new object[]
                        {
                            new[] { new { key = "0x" } }, // Query all storage keys (simplified - in production, use proper storage key prefixes)
                            null // block hash (null = latest)
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(queryRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync("", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (rpcResponse.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
                        {
                            var holons = new List<IHolon>();
                            foreach (var item in result.EnumerateArray())
                            {
                                if (item.TryGetProperty("value", out var value))
                                {
                                    // Decode the storage value (hex-encoded)
                                    var holonData = value.GetString();
                                    if (!string.IsNullOrEmpty(holonData))
                                    {
                                        // Parse holon from chain storage
                                        var holon = ParsePolkadotStorageToHolon(holonData);
                                        if (holon != null && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }

                            response.Result = holons;
                            response.IsError = false;
                            response.Message = $"Loaded {holons.Count} holons from Polkadot blockchain";
                        }
                        else
                        {
                            // Fallback: return empty list if no holons found
                            response.Result = new List<IHolon>();
                            response.IsError = false;
                            response.Message = "No holons found on Polkadot blockchain";
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to query Polkadot chain state: {httpResponse.StatusCode}");
                    }
                }
                else
                {
                    // Contract is configured - query holons from smart contract
                    var contractQueryRequest = new
                    {
                        id = 1,
                        jsonrpc = "2.0",
                        method = "state_call",
                        @params = new object[]
                        {
                            "Contracts",
                            "call",
                            new
                            {
                                dest = _contractAddress,
                                value = "0x0",
                                gasLimit = "0x100000",
                                storageDepositLimit = (object)null,
                                input = "0x" + System.Convert.ToHexString(Encoding.UTF8.GetBytes("get_all_holons"))
                            },
                            (string)null // block hash (null = latest)
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQueryRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync("", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (rpcResponse.TryGetProperty("result", out var result))
                        {
                            var resultData = result.GetProperty("data").GetString();
                            // Decode the contract call result
                            var holons = ParsePolkadotToHolons(resultData);
                            if (type != HolonType.All)
                            {
                                holons = holons.Where(h => h.HolonType == type);
                            }
                            response.Result = holons;
                            response.IsError = false;
                            response.Message = $"Loaded {holons.Count()} holons from Polkadot smart contract";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to parse contract call result");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to call Polkadot smart contract: {httpResponse.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolonsAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
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

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holon cannot be null");
                    return response;
                }

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - delegate to ProviderManager as fallback
                    return await HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                }

                // Serialize holon to Polkadot format
                string holonData = ConvertHolonToPolkadot(holon);
                string holonId = holon.Id.ToString();

                // Create Polkadot extrinsic to call smart contract
                // Note: This requires a deployed OASIS smart contract on Polkadot/Substrate
                var signedTx = await CreatePolkadotTransaction("save_holon", holonData);

                // Submit extrinsic to Polkadot network
                var submitRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[] { signedTx }
                };

                var jsonContent = JsonSerializer.Serialize(submitRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        // Store transaction hash in provider unique storage key
                        if (holon.ProviderUniqueStorageKey == null)
                            holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                        holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PolkadotOASIS] = result.GetString() ?? string.Empty;

                        response.Result = holon;
                        response.IsError = false;
                        response.IsSaved = true;
                        response.Message = $"Holon saved successfully to Polkadot: {result.GetString()}";

                        // Handle children if requested
                        if (saveChildren && holon.Children != null && holon.Children.Any())
                        {
                            var childResults = new List<OASISResult<IHolon>>();
                            foreach (var child in holon.Children)
                            {
                                var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                                childResults.Add(childResult);
                                
                                if (!continueOnError && childResult.IsError)
                                {
                                    OASISErrorHandling.HandleError(ref response, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                    return response;
                                }
                            }
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save holon to Polkadot - no transaction hash returned");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to submit Polkadot transaction: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonAsync: {ex.Message}", ex);
            }
            return response;
        }

    }
}
