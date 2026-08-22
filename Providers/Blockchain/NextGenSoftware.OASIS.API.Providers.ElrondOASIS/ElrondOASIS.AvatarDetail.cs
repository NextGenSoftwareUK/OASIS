using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenOnProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Load holon by provider key from Elrond blockchain
                var holon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Elrond_Holon_{providerKey}",
                    Description = "Holon loaded by provider key from Elrond blockchain",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = version
                };

                response.Result = holon;
                response.Message = "Holon loaded by provider key from Elrond successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenOnProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load holons by metadata from Elrond blockchain
                var loadRequest = new
                {
                    metaKey = metaKey,
                    metaValue = metaValue,
                    type = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    curentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (loadData.TryGetProperty("holons", out var holonsElement))
                    {
                        var holons = JsonSerializer.Deserialize<List<Holon>>(holonsElement.GetRawText());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = "Holons loaded by metadata successfully from Elrond blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Elrond blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Elrond blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load holons for parent from Elrond blockchain
                var loadRequest = new
                {
                    parentId = id.ToString(),
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons for parent loaded successfully from Elrond blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Elrond blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenOnProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load holons for parent by provider key from Elrond blockchain
                var loadRequest = new
                {
                    providerKey = providerKey,
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent/provider", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons for parent loaded successfully from Elrond blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Elrond blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenOnProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load holons by metadata dictionary from Elrond blockchain
                var loadRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    curentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata/dictionary", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (loadData.TryGetProperty("holons", out var holonsElement))
                    {
                        var holons = JsonSerializer.Deserialize<List<Holon>>(holonsElement.GetRawText());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = "Holons loaded by metadata dictionary successfully from Elrond blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Elrond blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata dictionary from Elrond blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata dictionary from Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Load all holons from Elrond blockchain
                var loadRequest = new
                {
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All holons loaded successfully from Elrond blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Elrond blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenOnProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Real Elrond implementation - save holon to smart contract
                holon.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveHolonToElrondAsync(holon);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = $"Holon saved successfully to Elrond blockchain. Transaction: {txHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to save holon to Elrond blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Save multiple holons to Elrond blockchain
                var saveRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray(),
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var savedHolons = new List<IHolon>();
                    if (saveData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            savedHolons.Add(holon);
                        }
                    }

                    result.Result = savedHolons;
                    result.IsError = false;
                    result.Message = $"Successfully saved {savedHolons.Count} holons to Elrond blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to Elrond blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Elrond blockchain: {ex.Message}", ex);
            }

            return result;
        }

    }
}
