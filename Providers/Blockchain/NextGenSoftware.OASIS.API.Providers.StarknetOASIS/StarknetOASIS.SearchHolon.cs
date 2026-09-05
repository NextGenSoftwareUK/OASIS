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
    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
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

            if (searchParams == null)
            {
                OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                return result;
            }

            // Extract search query
            string searchQuery = "";
            if (searchParams is ISearchTextGroup textGroup)
            {
                searchQuery = textGroup.SearchQuery ?? "";
            }

            // Search avatars and holons from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("search"),
                    calldata = new[] { searchQuery, version.ToString() }
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
                    var searchResults = new SearchResults();
                    
                    // Parse avatars
                    if (rpcResult.TryGetProperty("avatars", out var avatarsElement) && avatarsElement.ValueKind == JsonValueKind.Array)
                    {
                        var avatars = new List<IAvatar>();
                        foreach (var avatarElement in avatarsElement.EnumerateArray())
                        {
                            var avatar = ParseStarknetToAvatar(avatarElement);
                            if (avatar != null) avatars.Add(avatar);
                        }
                        searchResults.SearchResultAvatars = avatars;
                    }
                    
                    // Parse holons
                    if (rpcResult.TryGetProperty("holons", out var holonsElement) && holonsElement.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonElement in holonsElement.EnumerateArray())
                        {
                            var holon = ParseStarknetToHolon(holonElement);
                            if (holon != null) holons.Add(holon);
                        }
                        searchResults.SearchResultHolons = holons;
                    }
                    
                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = $"Successfully searched Starknet: found {searchResults.SearchResultAvatars?.Count() ?? 0} avatars and {searchResults.SearchResultHolons?.Count() ?? 0} holons";
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
            OASISErrorHandling.HandleError(ref result, $"Error searching Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

            // Query holon by ID from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_holon_by_id"),
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
            OASISErrorHandling.HandleError(ref result, $"Error loading holon from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }


    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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

            // Delete holon by ID from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_holon"),
                    calldata = new[] { id.ToString() }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                // Return the deleted holon (if available from response)
                result.IsError = false;
                result.Message = "Successfully deleted holon from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

            // Delete holon by provider key from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("delete_holon_by_provider_key"),
                    calldata = new[] { providerKey }
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                // Return the deleted holon (if available from response)
                result.IsError = false;
                result.Message = "Successfully deleted holon by provider key from Starknet";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Starknet RPC error: {httpResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

            // Query holons for parent by provider key from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_holons_for_parent_by_key"),
                    calldata = new[] { providerKey, type.ToString(), version.ToString() }
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
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

            // Query all holons from Starknet smart contract using RPC call
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_all_holons"),
                    calldata = new[] { type.ToString(), version.ToString() }
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
                    result.Message = $"Successfully loaded {holons.Count} holons from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

            // Query holons by metadata from Starknet smart contract using RPC call
            var metadataJson = JsonSerializer.Serialize(metaKeyValuePairs);
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_call",
                @params = new
                {
                    contract_address = _contractAddress,
                    entry_point_selector = GetEntryPointSelector("get_holons_by_metadata"),
                    calldata = new[] { metadataJson, metaKeyValuePairMatchMode.ToString(), type.ToString(), version.ToString() }
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
                    result.Message = $"Successfully loaded {holons.Count} holons by metadata from Starknet";
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
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var metaDict = new Dictionary<string, string> { { metaKey, metaValue } };
        return LoadHolonsByMetaDataAsync(metaDict, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

}
