using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent ID
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParent",
                        parameters = new[] { id.ToString(), type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent ID
                    // Search for files with parent ID in memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {id}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ParentHolonId == id && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract by provider key
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParentByProviderKey",
                        parameters = new[] { providerKey, type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent provider key
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey) && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKey = metaKey,
                    metaValue = metaValue,
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
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
                    result.Message = "Holons loaded by metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata key-value pairs from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata/multiple", content);
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
                    result.Message = "Holons loaded by multiple metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by multiple metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons from Hashgraph blockchain
                var loadRequest = new
                {
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
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
                    result.Message = "All holons loaded successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Serialize holon to JSON
                string holonInfo = JsonSerializer.Serialize(holon);
                int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                string holonId = holon.Id.ToString();

                // Use Hedera File Service or Smart Contract Service to store holon data via HTTP API
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Smart contract storage - use Hedera Smart Contract Service via REST API
                    var contractData = new
                    {
                        contractId = _contractAddress,
                        functionParameters = new
                        {
                            functionName = "createHolon",
                            parameters = new[]
                            {
                                new { type = "string", value = holonId },
                                new { type = "string", value = holonInfo }
                            }
                        }
                    };

                    var contractJson = JsonSerializer.Serialize(contractData);
                    var contractContent = new StringContent(contractJson, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", contractContent);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractResponseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(contractResponseContent);
                        
                        if (contractResult.TryGetProperty("transactionId", out var txId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = txId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hashgraph smart contract successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hashgraph smart contract: {await contractResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
                else
                {
                    // Use Hedera File Service via REST API
                    var fileData = new
                    {
                        contents = Encoding.UTF8.GetBytes(holonInfo),
                        fileMemo = $"OASIS Holon: {holonId}"
                    };

                    var fileJson = JsonSerializer.Serialize(new
                    {
                        contents = Convert.ToBase64String(fileData.contents),
                        fileMemo = fileData.fileMemo
                    });
                    var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                    var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                        
                        if (fileResult.TryGetProperty("fileId", out var fileId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hedera File Service successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Hashgraph: {ex.Message}", ex);
            }

            return result;
        }

    }
}
