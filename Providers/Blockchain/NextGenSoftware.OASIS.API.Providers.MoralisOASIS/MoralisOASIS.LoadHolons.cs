using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
// using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request; // Removed - use Requests (plural) instead
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS
{
    public partial class MoralisOASIS
    {
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, reloadChildren);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                    else
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                            return result;
                        }
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.IsSaved = savedHolons.Any();
                result.Message = errors.Any() 
                    ? $"Saved {savedHolons.Count} of {holons.Count()} holons. Errors: {string.Join("; ", errors)}"
                    : $"Successfully saved {savedHolons.Count} holons to Moralis IPFS";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons for parent from IPFS or contract
                // Try loading from contract first if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsForParent",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            parentId = id.ToString(),
                            holonType = holonType.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no results from contract, try IPFS directory listing
                if (holons.Count == 0)
                {
                    // List IPFS directory for parent holons
                    var listRequest = new
                    {
                        path = $"holons/parent_{id}/"
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(listRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/list", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var listResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (listResult.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in files.EnumerateArray())
                            {
                                if (file.TryGetProperty("path", out var filePath))
                                {
                                    var holonResult = await LoadHolonAsync(filePath.GetString(), loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version);
                                    if (!holonResult.IsError && holonResult.Result != null)
                                    {
                                        holons.Add(holonResult.Result);
                                        if (maxChildCount > 0 && holons.Count >= maxChildCount)
                                            break;
                                    }
                                    else if (!continueOnError)
                                    {
                                        OASISErrorHandling.HandleError(ref result, $"Failed to load holon {filePath.GetString()}: {holonResult.Message}");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons for parent {id} from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // First load the parent holon to get its ID
                var parentResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon with provider key {providerKey} not found");
                    return result;
                }

                // Use the parent's ID to load children
                return await LoadHolonsForParentAsync(parentResult.Result.Id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons by metadata from contract
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsByMetaData",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            metaKey = metaData,
                            metaValue = value,
                            holonType = holonType.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try loading all holons and filtering by metadata (less efficient)
                if (holons.Count == 0)
                {
                    var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, reloadChildren, version);
                    if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                    {
                        holons.AddRange(allHolonsResult.Result.Where(h => 
                            h.MetaData != null && 
                            h.MetaData.ContainsKey(metaData) && 
                            h.MetaData[metaData]?.ToString() == value));
                    }
                }

                // Apply maxChildCount limit
                if (maxChildCount > 0 && holons.Count > maxChildCount)
                {
                    holons = holons.Take(maxChildCount).ToList();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons by metadata ({metaData}={value}) from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons by metadata dictionary from contract
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsByMetaDataDict",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            metaData = JsonSerializer.Serialize(metaData),
                            matchMode = matchMode.ToString(),
                            holonType = holonType.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try loading all holons and filtering by metadata (less efficient)
                if (holons.Count == 0)
                {
                    var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, reloadChildren, version);
                    if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                    {
                        var filteredHolons = allHolonsResult.Result.Where(h =>
                        {
                            if (h.MetaData == null) return false;
                            
                            if (matchMode == MetaKeyValuePairMatchMode.All)
                            {
                                return metaData.All(kvp => 
                                    h.MetaData.ContainsKey(kvp.Key) && 
                                    h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                            }
                            else // Any
                            {
                                return metaData.Any(kvp => 
                                    h.MetaData.ContainsKey(kvp.Key) && 
                                    h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                            }
                        });
                        holons.AddRange(filteredHolons);
                    }
                }

                // Apply maxChildCount limit
                if (maxChildCount > 0 && holons.Count > maxChildCount)
                {
                    holons = holons.Take(maxChildCount).ToList();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons by metadata dictionary (matchMode={matchMode}) from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata dictionary: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holons = new List<IHolon>();

                // Real Moralis implementation - load all holons from contract or IPFS
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getAllHolons",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            holonType = holonType.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try IPFS directory listing
                if (holons.Count == 0)
                {
                    var listRequest = new
                    {
                        path = "holons/"
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(listRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/list", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var listResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (listResult.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in files.EnumerateArray())
                            {
                                if (file.TryGetProperty("path", out var filePath))
                                {
                                    var holonResult = await LoadHolonAsync(filePath.GetString(), loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version);
                                    if (!holonResult.IsError && holonResult.Result != null)
                                    {
                                        holons.Add(holonResult.Result);
                                        if (maxChildCount > 0 && holons.Count >= maxChildCount)
                                            break;
                                    }
                                    else if (!continueOnError)
                                    {
                                        OASISErrorHandling.HandleError(ref result, $"Failed to load holon {filePath.GetString()}: {holonResult.Message}");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex);
            }
            return result;
        }

    }
}
