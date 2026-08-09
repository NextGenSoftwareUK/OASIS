using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
    {
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 1,
                    key_type = "i64",
                    lower_bound = id.ToString(),
                    upper_bound = id.ToString(),
                    limit = 1
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holon = ParseTelosToHolon(responseContent);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from Telos";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos: {ex.Message}", ex);
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
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 2,
                    key_type = "str",
                    lower_bound = providerKey,
                    upper_bound = providerKey,
                    limit = 1
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holon = ParseTelosToHolon(responseContent);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from Telos by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain by provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos by provider key: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 3,
                    key_type = "i64",
                    lower_bound = id.ToString(),
                    upper_bound = id.ToString(),
                    limit = 100
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = ParseTelosToHolons(responseContent);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from Telos for parent";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 4,
                    key_type = "str",
                    lower_bound = providerKey,
                    upper_bound = providerKey,
                    limit = 100
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = ParseTelosToHolons(responseContent);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from Telos for parent by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent by provider key: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons and filter by metadata
                var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter by metadata
                var filteredHolons = allHolonsResult.Result?
                    .Where(h => h?.MetaData != null && 
                                h.MetaData.ContainsKey(metaKey) && 
                                h.MetaData[metaKey]?.ToString() == metaValue)
                    .ToList() ?? new List<IHolon>();

                result.Result = filteredHolons;
                result.IsError = false;
                result.Message = $"Found {filteredHolons.Count} holons matching metadata {metaKey}={metaValue}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons and filter by metadata
                var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter by metadata based on match mode
                IEnumerable<IHolon> filteredHolons = allHolonsResult.Result ?? new List<IHolon>();
                
                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                {
                    filteredHolons = filteredHolons.Where(h => h?.MetaData != null &&
                        metaKeyValuePairs.All(kvp => h.MetaData.ContainsKey(kvp.Key) && 
                                                     h.MetaData[kvp.Key]?.ToString() == kvp.Value));
                }
                else // MetaKeyValuePairMatchMode.Or
                {
                    filteredHolons = filteredHolons.Where(h => h?.MetaData != null &&
                        metaKeyValuePairs.Any(kvp => h.MetaData.ContainsKey(kvp.Key) && 
                                                     h.MetaData[kvp.Key]?.ToString() == kvp.Value));
                }

                result.Result = filteredHolons.ToList();
                result.IsError = false;
                result.Message = $"Found {result.Result.Count()} holons matching metadata";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        json = true,
                        code = "orgs.seeds",
                        scope = "orgs.seeds",
                        table = "holon",
                        limit = 1000, // Load up to 1000 holons
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array)
                    {
                        var holons = new List<IHolon>();
                        foreach (var row in rows.EnumerateArray())
                        {
                            try
                            {
                                var holon = ParseTelosToHolon(row.GetRawText());
                                if (holon != null && (type == HolonType.All || holon.HolonType == type))
                                {
                                    holons.Add(holon);
                                }
                            }
                            catch (Exception ex) when (continueOnError)
                            {
                                // Continue processing other holons on error
                                continue;
                            }
                        }

                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Loaded {holons.Count} holons from Telos blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse holons from Telos blockchain response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Save holon to Telos blockchain using real EOSIO smart contract
                var saveUrl = $"{TELOS_API_BASE_URL}/v1/chain/push_transaction";
                var saveData = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "orgs.seeds",
                            name = "upsertholon",
                            authorization = new[]
                            {
                                new { actor = "orgs.seeds", permission = "active" }
                            },
                            data = new
                            {
                                id = holon.Id.ToString(),
                                name = holon.Name ?? "",
                                description = holon.Description ?? "",
                                holon_type = holon.HolonType.ToString(),
                                parent_holon_id = holon.ParentHolonId != Guid.Empty ? holon.ParentHolonId.ToString() : "",
                                metadata = holon.MetaData != null ? JsonSerializer.Serialize(holon.MetaData) : "{}"
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(saveData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(saveUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("transaction_id", out var txId))
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = $"Holon saved to Telos blockchain successfully. Transaction ID: {txId.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse transaction response from Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Telos blockchain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                        if (!saveResult.IsError && saveResult.Result != null)
                        {
                            savedHolons.Add(saveResult.Result);
                        }
                        else if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                            return result;
                        }
                        else
                        {
                            errors.Add($"Holon {holon.Id}: {saveResult.Message}");
                        }
                    }
                    catch (Exception ex) when (continueOnError)
                    {
                        errors.Add($"Holon {holon?.Id}: {ex.Message}");
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Count > 0;
                result.Message = $"Saved {savedHolons.Count} of {holons.Count()} holons to Telos blockchain" + 
                    (errors.Count > 0 ? $". {errors.Count} errors occurred." : "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Telos: {ex.Message}", ex);
            }
            return result;
        }
    }
}
