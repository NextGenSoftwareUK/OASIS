using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Search on Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::search",
                        arguments = new[]
                        {
                            "", // SearchParams doesn't have SearchText property
                            "All", // SearchParams doesn't have SearchType property
                            version.ToString()
                        }
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
                        var searchResults = ParseAptosToSearchResults(result.GetRawText());
                        response.Result = searchResults;
                        response.IsError = false;
                        response.Message = "Search completed on Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No search results found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search on Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching on Aptos: {ex.Message}");
            }

            return response;
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
                var request = new
                {
                    function = "get_holon_by_id",
                    arguments = new[] { id.ToString() }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                    var holon = ParseAptosToHolon(jsonElement);
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Aptos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Aptos: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Aptos: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var request = new
                {
                    function = "get_holon_by_provider_key",
                    arguments = new[] { providerKey }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                    var holon = ParseAptosToHolon(jsonElement);
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Aptos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Aptos: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Aptos: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Query Aptos for holons with matching parent ID
                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_holons_by_parent",
                    arguments = new object[] { id.ToString(), (int)type },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null && (type == HolonType.All || holon.HolonType == type))
                            {
                                holons.Add(holon);
                            }
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons for parent from Aptos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found for parent on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from Aptos: {ex.Message}");
            }
            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey);
            if (parentResult.IsError || parentResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Parent holon with provider key {providerKey} not found");
                return response;
            }

            // Then load holons for that parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Query Aptos for holons matching metadata
                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_holons_by_metadata",
                    arguments = new object[] { metaKey, metaValue, (int)type },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null && (type == HolonType.All || holon.HolonType == type))
                            {
                                holons.Add(holon);
                            }
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons by metadata from Aptos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found with matching metadata on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from Aptos: {ex.Message}");
            }
            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Serialize metadata dictionary to JSON for query
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(metaKeyValuePairs);
                
                // Query Aptos for holons matching multiple metadata pairs
                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_holons_by_metadata_multi",
                    arguments = new object[] { metadataJson, metaKeyValuePairMatchMode.ToString(), (int)type },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null && (type == HolonType.All || holon.HolonType == type))
                            {
                                holons.Add(holon);
                            }
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons by metadata from Aptos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found with matching metadata on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from Aptos: {ex.Message}");
            }
            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Load all holons from Aptos blockchain using real Aptos RPC
                var request = new
                {
                    function = $"{APTOS_CONTRACT_ADDRESS}::oasis::get_all_holons",
                    arguments = new object[] { (int)type },
                    type_arguments = new object[0]
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/view", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var aptosResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (aptosResponse.TryGetProperty("0", out var holonsArray) && 
                        holonsArray.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonData in holonsArray.EnumerateArray())
                        {
                            var holon = ParseAptosToHolon(holonData);
                            if (holon != null)
                                holons.Add(holon);
                        }
                        
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse holons from Aptos response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Save holon to Aptos blockchain using real Aptos RPC
                var sequenceNumber = await GetSequenceNumber();
                var request = new
                {
                    sender = APTOS_ACCOUNT_ADDRESS,
                    sequence_number = sequenceNumber.ToString(),
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = ((DateTimeOffset)DateTime.UtcNow.AddMinutes(10)).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = $"{APTOS_CONTRACT_ADDRESS}::oasis::save_holon",
                        arguments = new object[]
                        {
                            holon.Id.ToString(),
                            holon.Name ?? "",
                            holon.Description ?? "",
                            (int)holon.HolonType,
                            holon.ParentHolonId.ToString(),
                            holon.ParentOmniverseId.ToString(),
                            holon.ParentMultiverseId.ToString(),
                            holon.ParentUniverseId.ToString(),
                            holon.ParentDimensionId.ToString(),
                            holon.ParentGalaxyClusterId.ToString(),
                            holon.ParentGalaxyId.ToString(),
                            holon.ParentSolarSystemId.ToString(),
                            holon.ParentPlanetId.ToString(),
                            holon.ParentMoonId.ToString(),
                            holon.ParentStarId.ToString(),
                            holon.ParentZomeId.ToString(),
                            holon.MetaData != null ? System.Text.Json.JsonSerializer.Serialize(holon.MetaData) : "",
                            ((DateTimeOffset)holon.CreatedDate).ToUnixTimeSeconds(),
                            ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                            holon.IsActive
                        },
                        type_arguments = new object[0]
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = holon;
                    response.IsError = false;
                    response.Message = "Holon saved to Aptos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"SaveHolonsAsync failed on holon {holon.Id}: {saveResult.Message}");
                            return result;
                        }
                    }
                    else
                        saved.Add(saveResult.Result);
                }
                result.Result = saved;
                result.IsSaved = true;
                result.Message = $"{saved.Count} holons saved to Aptos successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SaveHolonsAsync: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonAsync(id.ToString());
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // Load holon first to get its ID
                var loadResult = await LoadHolonAsync(providerKey);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon not found for provider key: {providerKey}");
                    return result;
                }
                
                // Delete holon using Aptos Move smart contract
                var deletePayload = new
                {
                    type = "entry_function_payload",
                    function = "0x1::oasis::delete_holon",
                    type_arguments = new string[0],
                    arguments = new[] { providerKey }
                };
                
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(deletePayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/transactions", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    result.Result = loadResult.Result;
                    result.Message = "Holon deleted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;
    }
}
